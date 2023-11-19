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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
#if NETCOREAPP
using HttpRequest = Microsoft.AspNetCore.Http.HttpRequest;
#else
using HttpRequest = System.Web.HttpRequestBase;
#endif

namespace WebQueryToolkit;

/// <summary>
/// Defines default values and configuration of query parameters.
/// This class is immutable.
/// </summary>
public partial class WebQuerySettings {

   /// <remarks>
   /// This setting is ignored by the query string parser.
   /// It's used by the table UI control to determine which columns
   /// to auto-generate filters for, where the name of the property is used as parameter name.
   /// </remarks>
   public ISet<string>
   FilterAllowedProperties { get; }

   /// <summary>
   /// The default sort expression, or <code>null</code> if no default was specified.
   /// </summary>
   public string?
   OrderBy { get; }

   /// <summary>
   /// The properties that are allowed to sort by. Empty means any property is allowed.
   /// </summary>
   public ISet<string>
   OrderByAllowedProperties { get; }

   /// <summary>
   /// <code>true</code> if the *orderby* parameter is allowed on the query string.
   /// </summary>
   public bool
   OrderByParameterAllowed { get; }

   /// <summary>
   /// The *orderby* parameter name. The default is "$orderby".
   /// </summary>
   public string
   OrderByParameterName { get; } = "$orderby";

   /// <summary>
   /// <code>true</code> if the *skip* (page start) parameter is allowed on the query string.
   /// </summary>
   public bool
   SkipParameterAllowed { get; }

   /// <summary>
   /// The *skip* (page start) parameter name. The default is "$skip".
   /// </summary>
   public string
   SkipParameterName { get; } = "$skip";

   /// <summary>
   /// The default *top* (page size) value, or <code>null</code> if no default was specified.
   /// </summary>
   public int?
   Top { get; }

   /// <summary>
   /// The max. allowed *top* (page size) value, or <code>null</code> if any value is allowed.
   /// Restricts the value that can be specified on the query string.
   /// </summary>
   public int?
   TopMax { get; }

   /// <summary>
   /// <code>true</code> if the *top* (page size) parameter is allowed on the query string.
   /// </summary>
   public bool
   TopParameterAllowed { get; }

   /// <summary>
   /// The *top* (page size) parameter name. The default is "$top".
   /// </summary>
   public string
   TopParameterName { get; } = "$top";

   /// <summary>
   /// Initializes a <see cref="WebQuerySettings"/> instance. All parameters are optional.
   /// </summary>
   /// <param name="filterAllowedProperties">
   /// <code>null</code> or empty means any property is allowed.
   /// </param>
   /// <param name="orderBy">The default sort expression, e.g. <code>"Id DESC"</code>.</param>
   /// <param name="orderByParameterAllowed">
   /// <code>true</code> if the *orderby* parameter is allowed on the query string.
   /// </param>
   /// <param name="orderByAllowedProperties">
   /// e.g. <code>new[] { "Id", "CreatedOn" }</code>.
   /// <code>null</code> or empty means any property is allowed.</param>
   /// <param name="orderByParameterName"><code>"$orderby"</code> is the default.</param>
   /// <param name="skipParameterAllowed">
   /// <code>true</code> if the *skip* (page start) parameter is allowed on the query string.
   /// </param>
   /// <param name="skipParameterName"><code>"$skip"</code> is the default.</param>
   /// <param name="top">The default page size. **You want to use this**.</param>
   /// <param name="topParameterAllowed">
   /// <code>true</code> if the *top* (page size) parameter is allowed on the query string.
   /// </param>
   /// <param name="topMax">
   /// The max. allowed page size, or <code>null</code> if any value is allowed.
   /// Restricts the value that can be specified on the query string.
   /// </param>
   /// <param name="topParameterName"><code>"$top"</code> is the default.</param>
   public
   WebQuerySettings(
         string[]? filterAllowedProperties = null,

         string? orderBy = null,
         bool orderByParameterAllowed = false,
         string[]? orderByAllowedProperties = null,
         string? orderByParameterName = null,

         bool skipParameterAllowed = false,
         string? skipParameterName = null,

         int? top = null,
         bool topParameterAllowed = false,
         int? topMax = null,
         string? topParameterName = null) {

      this.FilterAllowedProperties = new HashSet<string>(filterAllowedProperties ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

      if (!String.IsNullOrEmpty(orderBy)) {
         this.OrderBy = orderBy;
      }

      this.OrderByAllowedProperties = new HashSet<string>(orderByAllowedProperties ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
      this.OrderByParameterAllowed = orderByParameterAllowed;

      if (!String.IsNullOrEmpty(orderByParameterName)) {
         this.OrderByParameterName = orderByParameterName!;
      }

      this.SkipParameterAllowed = skipParameterAllowed;

      if (!String.IsNullOrEmpty(skipParameterName)) {
         this.SkipParameterName = skipParameterName!;
      }

      this.Top = top;
      this.TopMax = topMax;
      this.TopParameterAllowed = topParameterAllowed;

      if (!String.IsNullOrEmpty(topParameterName)) {
         this.TopParameterName = topParameterName!;
      }
   }
}

/// <summary>
/// Represents the query parameters to be used by your application. The values
/// may come from settings or from the query string.
/// </summary>
public class WebQueryParameters {

   readonly NameValueCollection?
   _urlQuery;

   static readonly char[]
   _pathEndDelimChars = { '?', '#' };

   /// <summary>
   /// The original URL. Used by <see cref="GetPageUrl"/> and <see cref="GetSortUrl"/>.
   /// </summary>
   public Uri
   Url { get; }

   /// <summary>
   /// The value of the *orderby* parameter if allowed/present,
   /// or the default value, or <code>null</code>.
   /// </summary>
   public string?
   OrderBy { get; }

   /// <summary>
   /// The value of the *skip* (page start) parameter if allowed/present, or 0.
   /// </summary>
   public int
   Skip { get; }

   /// <summary>
   /// The value of the *top* (page size) parameter if allowed/present,
   /// or the default value, or <code>null</code>.
   /// </summary>
   public int?
   Top { get; }

   /// <summary>
   /// The query settings that were used to create this instance.
   /// </summary>
   public WebQuerySettings
   Settings { get; }

   /// <summary>
   /// <code>true</code> if pagination is enabled
   /// (when *skip* parameter is allowed and a *top* value is available either
   /// from the parameter or from a default value).
   /// </summary>
   public bool
   PaginationEnabled =>
      Settings.SkipParameterAllowed
         && Top != null;

   /// <summary>
   /// Creates a <see cref="WebQueryParameters"/> instance by parsing the provided query string,
   /// and using the provided settings.
   /// </summary>
   /// <param name="url">The original URL.</param>
   /// <param name="urlQuery">The original query string.</param>
   /// <param name="settings">The query settings.</param>
   /// <param name="parameters">The requested instance, or <code>null</code>.</param>
   /// <param name="errorMessage">Used when a parameter is not allowed or out of range.</param>
   /// <returns><code>true</code> if <paramref name="parameters"/> is set to a new instance.</returns>
   public static bool
   TryCreate(Uri url, NameValueCollection urlQuery, WebQuerySettings settings, out WebQueryParameters? parameters, out string? errorMessage) {

      if (url is null) throw new ArgumentNullException(nameof(url));
      if (urlQuery is null) throw new ArgumentNullException(nameof(urlQuery));
      if (settings is null) throw new ArgumentNullException(nameof(settings));

      parameters = null;

      var orderBy = default(string);
      var skip = default(int?);
      var top = default(int?);

      static string disallowedParamError(string param) =>
         $"The {param} parameter is disallowed.";

      for (int i = 0; i < urlQuery.Keys.Count; i++) {

         var key = urlQuery.Keys[i];
         var values = urlQuery.GetValues(key)!;

         if (key == settings.OrderByParameterName) {

            if (!settings.OrderByParameterAllowed) {
               errorMessage = disallowedParamError(key);
               return false;
            }

            if ((orderBy = NonWhiteSpaceSingleValue(values, key, out errorMessage)) is null) {
               return false;
            }

            var orderByMatch = Regex.Match(orderBy, @"^(\w+)( desc)?$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

            if (!orderByMatch.Success) {

               errorMessage = $"Invalid {key}.";
               return false;
            }

            if (settings.OrderByAllowedProperties.Count > 0) {

               var orderByWithoutDir = orderByMatch.Groups[1].Value;

               if (!settings.OrderByAllowedProperties.Contains(orderByWithoutDir)) {

                  errorMessage = $"Sorting by '{orderByWithoutDir}' is disallowed.";
                  return false;
               }
            }

         } else if (key == settings.SkipParameterName) {

            if (!settings.SkipParameterAllowed) {
               errorMessage = disallowedParamError(key);
               return false;
            }

            if ((skip = Int32NonNegativeValue(values, key, out errorMessage)) is null) {
               return false;
            }

         } else if (key == settings.TopParameterName) {

            if (!settings.TopParameterAllowed) {
               errorMessage = disallowedParamError(key);
               return false;
            }

            if ((top = Int32NonNegativeValue(values, key, out errorMessage)) is null) {
               return false;
            }
         }
      }

      orderBy ??= settings.OrderBy;

      if (!top.HasValue
         && settings.Top.HasValue) {

         top = settings.Top.Value;
      }

      if (settings.TopMax.HasValue
         && top.HasValue
         && top > settings.TopMax) {

         errorMessage = $"{settings.TopParameterName} cannot be greater than {settings.TopMax}.";
         return false;
      }

      parameters = new WebQueryParameters(url, urlQuery, settings, orderBy, skip.GetValueOrDefault(), top);

      errorMessage = null;
      return true;
   }

   static int?
   Int32NonNegativeValue(string[] values, string parameterName, out string? errorMessage) {

      if (Int32Value(values, parameterName, out errorMessage) is int value) {

         if (value >= 0) {
            return value;
         }

         errorMessage = parameterName + " cannot be less than zero.";
         return null;
      }

      return null;
   }

   static int?
   Int32Value(string[] values, string parameterName, out string? errorMessage) {

      if (NonWhiteSpaceSingleValue(values, parameterName, out errorMessage) is string str) {

         if (Int32.TryParse(str, NumberStyles.None, CultureInfo.InvariantCulture, out int value)) {
            return value;
         }

         errorMessage = parameterName + " parameter must be a valid integer.";
         return null;
      }

      return null;
   }

   static string?
   NonWhiteSpaceSingleValue(string[] values, string parameterName, out string? errorMessage) {

      if (values.Length == 0) {
         errorMessage = null;
         return null;
      }

      if (values.Length == 1) {

         var value = values[0];

         if (!String.IsNullOrWhiteSpace(value)) {
            errorMessage = null;
            return value;
         }

         errorMessage = parameterName + " parameter cannot be empty.";
         return null;
      }

      errorMessage = parameterName + " parameter cannot be specified more than once.";
      return null;
   }

   /// <summary>
   /// Initializes a <see cref="WebQueryParameters"/> instance.
   /// Use this constructor if you parse the query string yourself.
   /// Otherwise, use <see cref="TryCreate"/> instead.
   /// </summary>
   /// <param name="url">The original URL.</param>
   /// <param name="urlQuery">The original query string.</param>
   public
   WebQueryParameters(
         Uri url,
         NameValueCollection? urlQuery,
         WebQuerySettings settings,
         string? orderBy,
         int skip,
         int? top) {

      this.Url = url ?? throw new ArgumentNullException(nameof(url));

      _urlQuery = (urlQuery != null) ?
         new NameValueCollection(urlQuery)
         : null;

      this.Settings = settings ?? throw new ArgumentNullException(nameof(settings));
      this.OrderBy = orderBy;
      this.Skip = skip;
      this.Top = top;
   }

   /// <summary>
   /// Creates a URL for the provided <paramref name="pageNumber"/>.
   /// </summary>
   /// <param name="pageNumber">The 1-based page number.</param>
   /// <param name="urlPath">
   /// The URL path. If <code>null</code>, <see cref="Url"/> is used.
   /// Use an empty string to never include the path.
   /// </param>
   /// <returns>The URL for a specific page.</returns>
   public string?
   GetPageUrl(int pageNumber, string? urlPath = null) {

      if (this.PaginationEnabled
         && pageNumber > 0) {

         var top = this.Top!.Value;
         var skip = (pageNumber * top) - top;

         var query = GetUrlQuery();
         query.Remove(this.Settings.SkipParameterName);

         if (skip > 0) {
            query[this.Settings.SkipParameterName] = skip.ToString(CultureInfo.InvariantCulture);
         }

         var path = urlPath ?? GetUrlPath();
         var queryString = ToQueryString(query, includeDelimiter: true);

         return String.Concat(path, queryString);
      }

      return null;
   }

   /// <summary>
   /// Creates a URL for the provided <paramref name="sortParam"/> (*orderby* parameter).
   /// </summary>
   /// <param name="sortParam">The value of the sort parameter.</param>
   /// <param name="urlPath">
   /// The URL path. If <code>null</code>, <see cref="Url"/> is used.
   /// Use an empty string to never include the path.
   /// </param>
   /// <returns>The URL for a specific sort value.</returns>
   public string?
   GetSortUrl(string sortParam, string? urlPath = null) {

      if (sortParam is null) throw new ArgumentNullException(nameof(sortParam));

      if (this.Settings.OrderByParameterAllowed) {

         var isDefaultSort = this.Settings.OrderBy?
            .Equals(sortParam, StringComparison.OrdinalIgnoreCase) == true;

         var query = GetUrlQuery();
         query.Remove(this.Settings.OrderByParameterName);

         if (this.Settings.SkipParameterAllowed) {
            query.Remove(this.Settings.SkipParameterName);
         }

         if (!isDefaultSort
            && sortParam.Length > 0) {

            query[this.Settings.OrderByParameterName] = sortParam;
         }

         var path = urlPath ?? GetUrlPath();
         var queryString = ToQueryString(query, includeDelimiter: true);

         return String.Concat(path, queryString);
      }

      return null;
   }

   string
   GetUrlPath() {

      if (this.Url.IsAbsoluteUri) {
         return this.Url.AbsolutePath;
      }

      var urlStr = this.Url.OriginalString;
      var pathEnd = urlStr.IndexOfAny(_pathEndDelimChars);

      if (pathEnd > -1) {
         return urlStr.Substring(0, pathEnd);
      }

      return urlStr;
   }

   NameValueCollection
   GetUrlQuery() {

      return (_urlQuery != null) ?
         new NameValueCollection(_urlQuery)
         : new NameValueCollection();
   }

   /// <summary>
   /// Checks if the query string contains the specified parameter.
   /// </summary>
   /// <param name="name">The name of the parameter to check.</param>
   /// <returns>true if the query string contains the parameter.</returns>
   public bool
   HasQueryParam(string name) {

      if (name is null) throw new ArgumentNullException(nameof(name));

      if (_urlQuery is null) {
         return false;
      }

      return _urlQuery[name] != null
         || _urlQuery.AllKeys.Contains(name);
   }

   /// <summary>
   /// Utility method to serialize a <see cref="NameValueCollection"/> as a query string.
   /// </summary>
   /// <param name="qs">The parameters.</param>
   /// <param name="includeDelimiter"><code>true</code> to include the "?" prefix.</param>
   /// <returns>A query string.</returns>
   public static string
   ToQueryString(NameValueCollection qs, bool includeDelimiter) {

      if (qs is null) throw new ArgumentNullException(nameof(qs));

      var sb = new StringBuilder();

      for (int i = 0; i < qs.AllKeys.Length; i++) {

         var key = qs.AllKeys[i];
         var values = qs.GetValues(key)!;

         if (values != null
            && values.Length > 0) {

            var encodedKey =
               (String.IsNullOrEmpty(key) || key[0] == '$') ? key
                  : HttpUtility.UrlEncode(key);

            for (int j = 0; j < values.Length; j++) {

               if (sb.Length > 0) {
                  sb.Append('&');
               }

               if (encodedKey != null) {
                  sb.Append(encodedKey)
                     .Append('=');
               }

               sb.Append(HttpUtility.UrlEncode(values[j]));
            }
         }
      }

      if (includeDelimiter
         && sb.Length > 0) {

         sb.Insert(0, '?');
      }

      return sb.ToString();
   }
}

/// <summary>
/// Wraps an <see cref="IEnumerable"/> to include pagination information.
/// This is an abstract class, to create an instance see <see cref="WebQueryResults{TResult}"/>.
/// </summary>
public abstract class WebQueryResults : IEnumerable {

   /// <summary>
   /// The number of elements of the current page.
   /// This property is computed after enumerating.
   /// </summary>
   public abstract int
   Count { get; }

   /// <summary>
   /// The number of all elements of the current query, if available.
   /// </summary>
   public int?
   TotalCount { get; }

   /// <summary>
   /// The query parameters used to obtain pagination information.
   /// </summary>
   public WebQueryParameters?
   QueryParameters { get; }

   /// <summary>
   /// The total number of pages, or 0 if pagination is not enabled.
   /// </summary>
   public int
   NumberOfPages { get; }

   /// <summary>
   /// The 1-based number of the current page, or 0 if pagination is not enabled.
   /// </summary>
   public int
   CurrentPage { get; }

   protected
   WebQueryResults(int? totalCount, WebQueryParameters? queryParameters) {

      this.TotalCount = totalCount;
      this.QueryParameters = queryParameters;

      if (this.QueryParameters != null) {

         var skip = this.QueryParameters.Skip;
         var top = this.QueryParameters.Top.GetValueOrDefault();

         this.NumberOfPages = (top == 0 || totalCount is null) ? 0
            : (int)Decimal.Ceiling(Decimal.Divide(totalCount.Value, top));

         this.CurrentPage = (top == 0) ? 0
            : (int)Decimal.Floor(Decimal.Divide(skip, top) + 1);
      }
   }

   public IEnumerator
   GetEnumerator() => BaseGetEnumerator();

   protected abstract IEnumerator
   BaseGetEnumerator();
}

/// <summary>
/// Wraps an <see cref="IEnumerable{TResult}"/> to include pagination information.
/// </summary>
public partial class WebQueryResults<TResult> : WebQueryResults, IEnumerable<TResult> {

   readonly IEnumerable<TResult>
   _results;

   int
   _count;

   bool
   _countComputed;

   /// <summary>
   /// The number of elements of the current page.
   /// This property is computed after enumerating.
   /// </summary>
   public override int
   Count {
      get {
         if (_results is ICollection<TResult> col) {
            return col.Count;
         }

         if (!_countComputed) {
            throw new InvalidOperationException("Count is available only after enumerating.");
         }

         return _count;
      }
   }

   /// <param name="results">The results sequence.</param>
   /// <param name="totalCount">The total number of elements of the query.</param>
   /// <param name="queryParameters">The query parameters.</param>
   public
   WebQueryResults(
         IEnumerable<TResult> results,
         int? totalCount = null,
         WebQueryParameters? queryParameters = null) : base(totalCount, queryParameters) {

      _results = results ?? throw new ArgumentNullException(nameof(results));
   }

   public new IEnumerator<TResult>
   GetEnumerator() {

      if (this.TotalCount is null
         || this.TotalCount > 0) {

         foreach (var item in _results) {
            _count++;
            yield return item;
         }
      }

      _countComputed = true;
   }

   protected override IEnumerator
   BaseGetEnumerator() => GetEnumerator();
}

/// <summary>
/// Defines extension methods that help create <see cref="N:WebQueryToolkit"/> objects
/// from other objects.
/// </summary>
public static partial class WebQueryExtensions {

   /// <summary>
   /// Tries to create a <see cref="WebQueryParameters"/> using the URL and query string
   /// from <paramref name="request"/>.
   /// </summary>
   public static bool
   TryCreateWebQueryParameters(
         this HttpRequest request,
         WebQuerySettings settings,
         out WebQueryParameters? parameters) {

      return WebQueryParameters.TryCreate(
         RequestUri(request),
         RequestQuery(request),
         settings,
         out parameters,
         out _
      );
   }

   static Uri
   RequestUri(HttpRequest request) {
#if NETCOREAPP
      var builder = new UriBuilder {
         Scheme = request.Scheme,
         Host = request.Host.Host,
         Port = request.Host.Port ?? -1,
         Path = request.Path,
         Query = request.QueryString.ToUriComponent()
      };

      return builder.Uri;
#else
      return request.Url;
#endif
   }

   static NameValueCollection
   RequestQuery(HttpRequest request) {
#if NETCOREAPP
      var query = request.Query;
      var nv = new NameValueCollection();

      foreach (var pair in query) {
         foreach (var item in pair.Value) {
            nv.Add(pair.Key, item);
         }
      }

      return nv;
#else
      return request.QueryString;
#endif
   }

   /// <summary>
   /// Fluent helper to create <see cref="WebQueryResults{TResult}"/> instances.
   /// </summary>
   public static WebQueryResults<TResult>
   ToWebQueryResults<TResult>(
         this IEnumerable<TResult> source,
         int? totalCount = null,
         WebQueryParameters? queryParameters = null) {

      return new WebQueryResults<TResult>(
         source,
         totalCount: totalCount,
         queryParameters: queryParameters
      );
   }

   /// <summary>
   /// Fluent helper to create <see cref="WebQueryResults{TResult}"/> instances.
   /// </summary>
   public static WebQueryResults<TResult>
   ToWebQueryResults<TResult>(
         this IEnumerable<TResult> source,
         WebQueryParameters? queryParameters) {

      return new WebQueryResults<TResult>(
         source,
         queryParameters: queryParameters
      );
   }
}
