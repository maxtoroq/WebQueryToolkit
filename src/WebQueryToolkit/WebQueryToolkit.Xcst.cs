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

namespace WebQueryToolkit;

using System;
using System.ComponentModel;
using System.Linq;

[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Class)]
public class WebQueryableAttribute : Attribute {

   int
   _top, _topMax;

   internal bool
   _topSet, _topMaxSet;

   public string[]?
   FilterAllowedProperties { get; set; }

   public string?
   OrderBy { get; set; }

   public string[]?
   OrderByAllowedProperties { get; set; }

   public bool
   OrderByParameterAllowed { get; set; }

   public string?
   OrderByParameterName { get; set; }

   public bool
   SkipParameterAllowed { get; set; }

   public string?
   SkipParameterName { get; set; }

   public int
   Top {
      get => _top;
      set {
         _top = value;
         _topSet = true;
      }
   }

   public int
   TopMax {
      get => _topMax;
      set {
         _topMax = value;
         _topMaxSet = true;
      }
   }

   public bool
   TopParameterAllowed { get; set; }

   public string?
   TopParameterName { get; set; }
}

partial class WebQuerySettings {

   /// <summary>
   /// Creates <see cref="WebQuerySettings"/> for the provided annotated <paramref name="type"/>.
   /// </summary>
   public static WebQuerySettings
   ForType(Type type) {

      if (type is null) throw new ArgumentNullException(nameof(type));

      WebQueryableAttribute attr = type
         .GetCustomAttributes(typeof(WebQueryableAttribute), inherit: true)
         .Cast<WebQueryableAttribute>()
         .SingleOrDefault()
         ?? throw new InvalidOperationException($"The type {type.FullName} is not annotated. Use wqt:queryable='yes'.");

      return new WebQuerySettings(
         filterAllowedProperties: attr.FilterAllowedProperties,
         orderBy: attr.OrderBy,
         orderByAllowedProperties: attr.OrderByAllowedProperties,
         orderByParameterAllowed: attr.OrderByParameterAllowed,
         orderByParameterName: attr.OrderByParameterName,
         skipParameterAllowed: attr.SkipParameterAllowed,
         skipParameterName: attr.SkipParameterName,
         top: (attr._topSet) ? attr.Top : default(int?),
         topMax: (attr._topMaxSet) ? attr.TopMax : default(int?),
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
   public Func<object, string>
   rowUrlFn(Func<TResult, string> urlFn) {

      if (urlFn is null) throw new ArgumentNullException(nameof(urlFn));

      return o => urlFn((TResult)o);
   }
}
