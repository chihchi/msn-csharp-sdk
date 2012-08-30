//-----------------------------------------------------------------------
// <copyright file="MsnBatchParameter.cs" company="The Outercurve Foundation">
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
    using System.ComponentModel;

    /// <summary>
    /// Represents a batch parameter for the creating batch requests.
    /// </summary>
    /// <remarks>
    /// http://developers.msn.com/docs/api/batch/
    /// </remarks>
    public class MsnBatchParameter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MsnBatchParameter"/> class.
        /// </summary>
        public MsnBatchParameter()
        {
            HttpMethod = HttpMethod.Get;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MsnBatchParameter"/> class.
        /// </summary>
        /// <param name="path">
        /// The resource path.
        /// </param>
        public MsnBatchParameter(string path)
            : this(HttpMethod.Get, path)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MsnBatchParameter"/> class.
        /// </summary>
        /// <param name="httpMethod">
        /// The http method.
        /// </param>
        /// <param name="path">
        /// The resource path.
        /// </param>
        public MsnBatchParameter(HttpMethod httpMethod, string path)
            : this(httpMethod, path, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MsnBatchParameter"/> class.
        /// </summary>
        /// <param name="path">
        /// The resource path.
        /// </param>
        /// <param name="parameters">
        /// The parameters.
        /// </param>
        public MsnBatchParameter(string path, object parameters)
            : this(HttpMethod.Get, path, parameters)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MsnBatchParameter"/> class.
        /// </summary>
        /// <param name="httpMethod">
        /// The http method.
        /// </param>
        /// <param name="path">
        /// The resource path.
        /// </param>
        /// <param name="parameters">
        /// The parameters.
        /// </param>
        public MsnBatchParameter(HttpMethod httpMethod, string path, object parameters)
        {
            HttpMethod = httpMethod;
            Path = path;
            Parameters = parameters;
        }

        /// <summary>
        /// Gets or sets the http method.
        /// </summary>
        public HttpMethod HttpMethod { get; set; }

        /// <summary>
        /// Gets or sets the resource path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the parameters.
        /// </summary>
        public object Parameters { get; set; }

        /// <summary>
        /// Gets or sets the raw data parameter.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object Data { get; set; }
    }
}