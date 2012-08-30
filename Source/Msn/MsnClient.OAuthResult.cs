//-----------------------------------------------------------------------
// <copyright file="MsnClient.OAuthResult.cs" company="The Outercurve Foundation">
//    Copyright (c) 2011, The Outercurve Foundation. 
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
// <author>Nathan Totten (ntotten.com), Jim Zimmerman (jimzimmerman.com) and Prabir Shrestha (prabir.me)</author>
// <website>https://github.com/msn-csharp-sdk/facbook-csharp-sdk</website>
//-----------------------------------------------------------------------

namespace Msn
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;

    public partial class MsnClient
    {
        /// <summary>
        /// Try parsing the url to <see cref="MsnOAuthResult"/>.
        /// </summary>
        /// <param name="url">The url to parse</param>
        /// <param name="msnOAuthResult">The msn oauth result.</param>
        /// <returns>True if parse successful, otherwise false.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]        
        public virtual bool TryParseOAuthCallbackUrl(Uri url, out MsnOAuthResult msnOAuthResult)
        {
            msnOAuthResult = null;

            try
            {
                msnOAuthResult = ParseOAuthCallbackUrl(url);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Parse the url to <see cref="MsnOAuthResult"/>.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [SuppressMessage("Microsoft.Naming", "CA2204:LiteralsShouldBeSpelledCorrectly")]
        public virtual MsnOAuthResult ParseOAuthCallbackUrl(Uri uri)
        {
            var parameters = new Dictionary<string, object>();

            bool found = false;
            if (!string.IsNullOrEmpty(uri.Fragment))
            {
                // #access_token and expries_in are in fragment
                var fragment = uri.Fragment.Substring(1);
                ParseUrlQueryString("?" + fragment, parameters, true);

                if (parameters.ContainsKey("access_token"))
                    found = true;
            }

            // code, state, error_reason, error and error_description are in query
            // ?error_reason=user_denied&error=access_denied&error_description=The+user+denied+your+request.
            var queryPart = new Dictionary<string, object>();
            ParseUrlQueryString(uri.Query, queryPart, true);

            if (queryPart.ContainsKey("code") || (queryPart.ContainsKey("error") && queryPart.ContainsKey("error_description")))
                found = true;

            foreach (var kvp in queryPart)
                parameters[kvp.Key] = kvp.Value;

            if (found)
                return new MsnOAuthResult(parameters);

            throw new InvalidOperationException("Could not parse Msn OAuth url.");
        }

        /// <summary>
        /// Gets the Msn OAuth login url.
        /// </summary>
        /// <param name="parameters">
        /// The parameters.
        /// </param>
        /// <returns>
        /// The login url.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If parameters is null.
        /// </exception>
        public virtual Uri GetLoginUrl(object parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException("parameters");

            IDictionary<string, MsnMediaObject> mediaObjects;
            IDictionary<string, MsnMediaStream> mediaStreams;
            var dictionary = ToDictionary(parameters, out mediaObjects, out mediaStreams) ?? new Dictionary<string, object>();

            bool isMobile = false;
            if (dictionary.ContainsKey("mobile"))
            {
                isMobile = (bool)dictionary["mobile"];
                dictionary.Remove("mobile");
            }

            var sb = new StringBuilder();
            sb.Append("https://login.live.com/oauth20_authorize.srf?");

            foreach (var kvp in dictionary)
                sb.AppendFormat("{0}={1}&", HttpHelper.UrlEncode(kvp.Key), HttpHelper.UrlEncode(BuildHttpQuery(kvp.Value, HttpHelper.UrlEncode)));

            sb.Length--;

            return new Uri(sb.ToString());
        }

        /// <summary>
        /// Gets the Msn OAuth logout url.
        /// </summary>
        /// <param name="parameters">
        /// The parameters.
        /// </param>
        /// <returns>
        /// The logout url.
        /// </returns>
        public virtual Uri GetLogoutUrl(object parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException("parameters");

            IDictionary<string, MsnMediaObject> mediaObjects;
            IDictionary<string, MsnMediaStream> mediaStreams;
            var dictionary = ToDictionary(parameters, out mediaObjects, out mediaStreams);

            var sb = new StringBuilder();
            sb.Append("https://login.live.com/oauth20_logout.srf?");

            if (dictionary != null)
            {
                foreach (var kvp in dictionary)
                    sb.AppendFormat("{0}={1}&", HttpHelper.UrlEncode(kvp.Key), HttpHelper.UrlEncode(BuildHttpQuery(kvp.Value, HttpHelper.UrlEncode)));
            }

            sb.Length--;

            return new Uri(sb.ToString());
        }
    }
}