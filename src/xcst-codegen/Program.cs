﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xcst.Compiler;

namespace XcstCodeGen {

   using static Path;

   class Program {

      const string FileExt = "xcst";

      private Uri CallerBaseUri { get; }

      private Uri ProjectUri { get; }

      private string Configuration { get; }

      private bool LibsAndPages { get; }

      public Program(Uri callerBaseUri, Uri projectUri, string configuration, bool libsAndPages) {

         this.CallerBaseUri = callerBaseUri;
         this.ProjectUri = projectUri;
         this.Configuration = configuration;
         this.LibsAndPages = libsAndPages;
      }

      // Loading project dependencies enables referencing packages from other projects
      void LoadProjectDependencies(XDocument projectDoc) {

         XNamespace xmlns = projectDoc.Root.Name.Namespace;
         XName itemGroupName = xmlns + "ItemGroup";

         foreach (XElement projectRef in projectDoc.Root.Elements(xmlns + "ItemGroup").Elements(xmlns + "ProjectReference")) {

            string refDir = GetDirectoryName(projectRef.Attribute("Include").Value);
            string refDll = Combine(refDir, "bin", Configuration, projectRef.Element(xmlns + "Name").Value + ".dll");
            var refDllUri = new Uri(ProjectUri, refDll);

            if (File.Exists(refDllUri.LocalPath)) {
               Assembly.LoadFrom(refDllUri.LocalPath);
            }
         }
      }

      // Transforms invalid identifier (class, namespace, variable) characters
      string CleanIdentifier(string identifier) =>
         Regex.Replace(identifier, "[^a-z0-9_.]", "_", RegexOptions.IgnoreCase);

      // Show compilation errors on Visual Studio's Error List
      // Also makes the error on the Output window clickable
      void VisualStudioErrorLog(CompileException ex) {

         string uriString = ex.ModuleUri;
         var uri = new Uri(uriString);
         string path = (uri.IsFile) ? uri.LocalPath : uriString;

         Console.WriteLine($"{path}({ex.LineNumber}): XCST error {ex.ErrorCode}: {ex.Message}");
      }

      void Run(TextWriter output) {

         var compilerFact = new XcstCompilerFactory {
            EnableExtensions = true
         };

         // Enable "application" extension
         compilerFact.RegisterExtensionsForAssembly(typeof(Xcst.Web.Extension.ExtensionLoader).Assembly);

         XDocument projectDoc = XDocument.Load(ProjectUri.LocalPath);
         XNamespace xmlns = projectDoc.Root.Name.Namespace;

         LoadProjectDependencies(projectDoc);

         output.WriteLine("//------------------------------------------------------------------------------");
         output.WriteLine("// <auto-generated>");
         output.WriteLine("//     This code was generated by a tool.");
         output.WriteLine("//");
         output.WriteLine("//     Changes to this file may cause incorrect behavior and will be lost if");
         output.WriteLine("//     the code is regenerated.");
         output.WriteLine("// </auto-generated>");
         output.WriteLine("//------------------------------------------------------------------------------");
         output.WriteLine();
         output.WriteLine("#nullable enable");

         if (LibsAndPages) {
            output.WriteLine();
            output.WriteLine("[assembly: AspNetPrecompiled.Infrastructure.PrecompiledModule]");
         }

         var startUri = new Uri(ProjectUri, ".");

         string rootNamespace = projectDoc.Root
            .Elements(xmlns + "PropertyGroup")
            .First()
            .Element(xmlns + "RootNamespace")
            .Value;

         foreach (string file in Directory.EnumerateFiles(startUri.LocalPath, "*." + FileExt, SearchOption.AllDirectories)) {

            var fileUri = new Uri(file, UriKind.Absolute);
            string fileName = GetFileName(file);
            string fileBaseName = GetFileNameWithoutExtension(file);

            // Ignore files starting with underscore
            if (fileName[0] == '_') {
               continue;
            }

            XcstCompiler compiler = compilerFact.CreateCompiler();
            compiler.PackagesLocation = startUri.LocalPath;
            compiler.PackageFileExtension = FileExt;
            compiler.IndentChars = "   ";

            string relativePath = startUri.MakeRelativeUri(fileUri).OriginalString;

            // Treat files ending with 'Package' as library packages; other files as pages
            // Library packages must be rooted at <c:package> and have a name
            // Pages must NOT be named
            // An alternative would be to use different file extensions for library packages and pages
            bool isPage = LibsAndPages
               && !fileBaseName.EndsWith("Package");

            if (isPage) {

               string ns = rootNamespace;

               if (relativePath.Contains("/")) {

                  string relativeDir = startUri.MakeRelativeUri(new Uri(GetDirectoryName(file), UriKind.Absolute))
                     .OriginalString;

                  ns = ns + "." + CleanIdentifier(relativeDir.Replace("/", "."));
               }

               compiler.TargetClass = "_Page_" + CleanIdentifier(fileBaseName);
               compiler.TargetNamespace = ns;
               compiler.TargetBaseTypes = new[] { "global::AspNetPrecompiled.AppPage" };
               compiler.TargetVisibility = "internal";

               // Sets a:application-uri, used to generate Href() functions for each module
               //compiler.SetParameter("http://maxtoroq.github.io/XCST/application", "application-uri", startUri);

            } else {
               compiler.NamedPackage = true;
            }

            CompileResult xcstResult;

            try {
               xcstResult = compiler.Compile(fileUri);

            } catch (CompileException ex) {
               VisualStudioErrorLog(ex);
               throw;
            }

            foreach (string cu in xcstResult.CompilationUnits) {
               output.Write(cu);
            }

            if (isPage) {

               string pagePath = ChangeExtension(relativePath, null);

               output.WriteLine();
               output.WriteLine();
               output.WriteLine($"namespace {compiler.TargetNamespace} {{");
               output.WriteLine(compiler.IndentChars + $"[global::AspNetPrecompiled.Infrastructure.VirtualPath(\"{pagePath}\")]");
               output.WriteLine(compiler.IndentChars + $"partial class {compiler.TargetClass} {{");
               output.WriteLine(compiler.IndentChars + compiler.IndentChars + "public static string LinkTo(params object[] pathParts) {");

               if (fileBaseName == "index") {
                  string defaultPagePath = pagePath.Substring(0, pagePath.Length - "index".Length).TrimEnd('/');
                  output.WriteLine($"{compiler.IndentChars + compiler.IndentChars + compiler.IndentChars}return global::AspNetPrecompiled.Infrastructure.LinkToHelper.LinkToDefault(\"/{pagePath}\", \"/{defaultPagePath}\", pathParts);");
               } else {
                  output.WriteLine($"{compiler.IndentChars + compiler.IndentChars + compiler.IndentChars}return global::AspNetPrecompiled.Infrastructure.LinkToHelper.LinkTo(\"/{pagePath}\", pathParts);");
               }

               output.WriteLine(compiler.IndentChars + compiler.IndentChars + "}");
               output.WriteLine(compiler.IndentChars + "}");
               output.Write("}");
            }
         }
      }

      public static void Main(string[] args) {

         string currentDir = Environment.CurrentDirectory;

         if (currentDir.Last() != DirectorySeparatorChar) {
            currentDir += DirectorySeparatorChar;
         }

         var callerBaseUri = new Uri(currentDir, UriKind.Absolute);
         var projectUri = new Uri(callerBaseUri, args[0]);
         string config = args[1];

         bool libsAndPages = (args.Length > 2) ?
            args[2] == "-LibsAndPages"
            : false;

         var baseUri = new Uri(AppDomain.CurrentDomain.BaseDirectory, UriKind.Absolute);
         var outputBase = new Uri(baseUri, "../../");
         var outputUri = new Uri(projectUri, "xcst.generated.cs");

         using (var output = File.CreateText(outputUri.LocalPath)) {

            // Because XML parsers normalize CRLF to LF, we want to be consistent with the additional content we create
            output.NewLine = "\n";

            new Program(callerBaseUri, projectUri, config, libsAndPages)
               .Run(output);
         }
      }
   }
}
