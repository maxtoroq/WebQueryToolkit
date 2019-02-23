// Copyright 2018 Max Toro Q.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

/* This file defines types/members used by WebQueryPackage (XCST).
 * If you don't use WebQueryPackage you can ignore or remove.
 */

[assembly: System.Web.PreApplicationStartMethod(
   typeof(WebQueryToolkit.PreApplicationStartCode),
   nameof(WebQueryToolkit.PreApplicationStartCode.Start))]

[assembly: Xcst.Compiler.XcstExtension(
   "http://maxtoroq.github.io/WebQueryToolkit",
   typeof(WebQueryToolkit.ExtensionLoader))]

namespace WebQueryToolkit {

   using System;
   using System.ComponentModel;
   using System.IO;
   using System.Linq;
   using Xcst.Compiler;
   using Xcst.Web.Compilation;

   [EditorBrowsable(EditorBrowsableState.Never)]
   [AttributeUsage(AttributeTargets.Class)]
   public class WebQueryableAttribute : Attribute {

      int _Top, _TopMax;
      internal bool topSet, topMaxSet;

      public string[] FilterAllowedProperties { get; set; }

      public string OrderBy { get; set; }

      public string[] OrderByAllowedProperties { get; set; }

      public bool OrderByParameterAllowed { get; set; }

      public string OrderByParameterName { get; set; }

      public bool SkipParameterAllowed { get; set; }

      public string SkipParameterName { get; set; }

      public int Top {
         get => _Top;
         set {
            _Top = value;
            topSet = true;
         }
      }

      public int TopMax {
         get => _TopMax;
         set {
            _TopMax = value;
            topMaxSet = true;
         }
      }

      public bool TopParameterAllowed { get; set; }

      public string TopParameterName { get; set; }
   }

   partial class WebQuerySettings {

      /// <summary>
      /// Creates <see cref="WebQuerySettings"/> for the provided annotated <paramref name="type"/>.
      /// </summary>

      public static WebQuerySettings ForType(Type type) {

         if (type == null) throw new ArgumentNullException(nameof(type));

         WebQueryableAttribute attr = type
            .GetCustomAttributes(typeof(WebQueryableAttribute), inherit: true)
            .Cast<WebQueryableAttribute>()
            .SingleOrDefault();

         if (attr == null) {
            throw new InvalidOperationException($"The type {type.FullName} is not annotated. Use wqt:queryable='yes'.");
         }

         return new WebQuerySettings(
            filterAllowedProperties: attr.FilterAllowedProperties,
            orderBy: attr.OrderBy,
            orderByAllowedProperties: attr.OrderByAllowedProperties,
            orderByParameterAllowed: attr.OrderByParameterAllowed,
            orderByParameterName: attr.OrderByParameterName,
            skipParameterAllowed: attr.SkipParameterAllowed,
            skipParameterName: attr.SkipParameterName,
            top: (attr.topSet) ? attr.Top : default(int?),
            topMax: (attr.topMaxSet) ? attr.TopMax : default(int?),
            topParameterAllowed: attr.TopParameterAllowed,
            topParameterName: attr.TopParameterName
         );
      }
   }

   partial class WebQueryResults<TResult> {

      /// <summary>
      /// Converts a function delegate to the appropiate type expected
      /// by the table (grid) control.
      /// </summary>

      public Func<object, string> rowUrlFn(Func<TResult, string> urlFn) {

         if (urlFn == null) throw new ArgumentNullException(nameof(urlFn));

         return o => urlFn((TResult)o);
      }
   }

   class ExtensionLoader : XcstExtensionLoader {

      public override Stream LoadSource() {

         return typeof(ExtensionLoader).Assembly
            .GetManifestResourceStream($"{nameof(WebQueryToolkit)}.WebQueryToolkit.xsl");
      }
   }

   /// <exclude/>

   [EditorBrowsable(EditorBrowsableState.Never)]
   public static class PreApplicationStartCode {

      static bool startWasCalled;

      public static void Start() {

         if (!startWasCalled) {

            startWasCalled = true;

            PageBuildProvider.CompilerFactory.RegisterExtensionsForAssembly(typeof(PreApplicationStartCode).Assembly);
         }
      }
   }
}
