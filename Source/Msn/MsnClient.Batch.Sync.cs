//-----------------------------------------------------------------------
// <copyright file="MsnClient.Batch.Sync.cs" company="The Outercurve Foundation">
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
    public partial class MsnClient
    {
        /// <summary>
        /// Makes a batch request to the Msn server.
        /// </summary>
        /// <param name="batchParameters">
        /// List of batch parameters.
        /// </param>
        /// <returns>
        /// The json result.
        /// </returns>
        public virtual object Batch(params MsnBatchParameter[] batchParameters)
        {
            return Batch(batchParameters, null);
        }

        /// <summary>
        /// Makes a batch request to the Msn server.
        /// </summary>
        /// <param name="batchParameters">List of batch parameters.</param>
        /// <param name="parameters">The parameters</param>
        /// <returns>The json result.</returns>
        public virtual object Batch(MsnBatchParameter[] batchParameters, object parameters)
        {
            var actualParameter = PrepareBatchRequest(batchParameters, parameters);
            return Post(actualParameter);
        }
    }
}