﻿//-----------------------------------------------------------------------
// <copyright file="MsnClient.Async.cs" company="The Outercurve Foundation">
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
#if FLUENTHTTP_CORE_TPL
    using System.ComponentModel;
#endif
    using System.Diagnostics.CodeAnalysis;
#if NETFX_CORE
    using System.Linq;
#endif
    using System.IO;
    using System.Net;

    [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    public partial class MsnClient
    {
        private HttpWebRequestWrapper _httpWebRequest;
        private object _httpWebRequestLocker = new object();

        /// <summary>
        /// Event handler for get completion.
        /// </summary>
        public event EventHandler<MsnApiEventArgs> GetCompleted;

        /// <summary>
        /// Event handler for post completion.
        /// </summary>
        public event EventHandler<MsnApiEventArgs> PostCompleted;

        /// <summary>
        /// Event handler for delete completion.
        /// </summary>
        public event EventHandler<MsnApiEventArgs> DeleteCompleted;

        /// <summary>
        /// Event handler for upload progress changed.
        /// </summary>
        public event EventHandler<MsnUploadProgressChangedEventArgs> UploadProgressChanged;

#if FLUENTHTTP_CORE_TPL

        /// <summary>
        /// Event handler when http web request wrapper is created for async api only.
        /// (used internally by TPL for cancellation support)
        /// </summary>
        private event EventHandler<HttpWebRequestCreatedEventArgs> HttpWebRequestWrapperCreated;

#endif

        /// <summary>
        /// Cancels asynchronous requests.
        /// </summary>
        /// <remarks>
        /// Does not cancel requests created using XTaskAsync methods.
        /// </remarks>
        public virtual void CancelAsync()
        {
            lock (_httpWebRequestLocker)
            {
                if (_httpWebRequest != null)
                    _httpWebRequest.Abort();
            }
        }

        /// <summary>
        /// Makes an asynchronous request to the Msn server.
        /// </summary>
        /// <param name="httpMethod">Http method. (GET/POST/DELETE)</param>
        /// <param name="path">The resource path or the resource url.</param>
        /// <param name="parameters">The parameters</param>
        /// <param name="resultType">The type of deserialize object into.</param>
        /// <param name="userState">The user state.</param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
#if FLUENTHTTP_CORE_TPL
        [Obsolete("Use ApiTaskAsync instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        protected virtual void ApiAsync(HttpMethod httpMethod, string path, object parameters, Type resultType, object userState)
        {
            Stream input;
            bool containsEtag;
            IList<int> batchEtags = null;
            var httpHelper = PrepareRequest(httpMethod, path, parameters, resultType, out input, out containsEtag, out batchEtags);
            _httpWebRequest = httpHelper.HttpWebRequest;

#if FLUENTHTTP_CORE_TPL
            if (HttpWebRequestWrapperCreated != null)
                HttpWebRequestWrapperCreated(this, new HttpWebRequestCreatedEventArgs(userState, httpHelper.HttpWebRequest));
#endif

            var uploadProgressChanged = UploadProgressChanged;
            bool notifyUploadProgressChanged = uploadProgressChanged != null && httpHelper.HttpWebRequest.Method == "POST";

            httpHelper.OpenReadCompleted +=
                (o, e) =>
                {
                    MsnApiEventArgs args;
                    if (e.Cancelled)
                    {
                        args = new MsnApiEventArgs(e.Error, true, userState, null);
                    }
                    else if (e.Error == null)
                    {
                        string responseString = null;

                        try
                        {
                            using (var stream = e.Result)
                            {
#if NETFX_CORE
                                bool compressed = false;
                                
                                var contentEncoding = httpHelper.HttpWebResponse.Headers.AllKeys.Contains("Content-Encoding") ? httpHelper.HttpWebResponse.Headers["Content-Encoding"] : null;
                                if (contentEncoding != null)
                                {
                                    if (contentEncoding.IndexOf("gzip") != -1)
                                    {
                                        using (var uncompressedStream = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Decompress))
                                        {
                                            using (var reader = new StreamReader(uncompressedStream))
                                            {
                                                responseString = reader.ReadToEnd();
                                            }
                                        }

                                        compressed = true;
                                    }
                                    else if (contentEncoding.IndexOf("deflate") != -1)
                                    {
                                        using (var uncompressedStream = new System.IO.Compression.DeflateStream(stream, System.IO.Compression.CompressionMode.Decompress))
                                        {
                                            using (var reader = new StreamReader(uncompressedStream))
                                            {
                                                responseString = reader.ReadToEnd();
                                            }
                                        }

                                        compressed = true;
                                    }
                                }

                                if (!compressed)
                                {
                                    using (var reader = new StreamReader(stream))
                                    {
                                        responseString = reader.ReadToEnd();
                                    }
                                }
#else
                                var response = httpHelper.HttpWebResponse;
                                if (response != null && response.StatusCode == HttpStatusCode.NotModified)
                                {
                                    var jsonObject = new JsonObject();
                                    var headers = new JsonObject();

                                    foreach (var headerName in response.Headers.AllKeys)
                                        headers[headerName] = response.Headers[headerName];

                                    jsonObject["headers"] = headers;
                                    args = new MsnApiEventArgs(null, false, userState, jsonObject);
                                    OnCompleted(httpMethod, args);
                                    return;
                                }

                                using (var reader = new StreamReader(stream))
                                {
                                    responseString = reader.ReadToEnd();
                                }
#endif
                            }

                            try
                            {
                                object result = ProcessResponse(httpHelper, responseString, resultType, containsEtag, batchEtags);
                                args = new MsnApiEventArgs(null, false, userState, result);
                            }
                            catch (Exception ex)
                            {
                                args = new MsnApiEventArgs(ex, false, userState, null);
                            }
                        }
                        catch (Exception ex)
                        {
                            args = httpHelper.HttpWebRequest.IsCancelled ? new MsnApiEventArgs(ex, true, userState, null) : new MsnApiEventArgs(ex, false, userState, null);
                        }
                    }
                    else
                    {
                        var webEx = e.Error as WebExceptionWrapper;
                        if (webEx == null)
                        {
                            args = new MsnApiEventArgs(e.Error, httpHelper.HttpWebRequest.IsCancelled, userState, null);
                        }
                        else
                        {
                            if (webEx.GetResponse() == null)
                            {
                                args = new MsnApiEventArgs(webEx, false, userState, null);
                            }
                            else
                            {
                                var response = httpHelper.HttpWebResponse;
                                if (response.StatusCode == HttpStatusCode.NotModified)
                                {
                                    var jsonObject = new JsonObject();
                                    var headers = new JsonObject();

                                    foreach (var headerName in response.Headers.AllKeys)
                                        headers[headerName] = response.Headers[headerName];

                                    jsonObject["headers"] = headers;
                                    args = new MsnApiEventArgs(null, false, userState, jsonObject);
                                }
                                else
                                {
                                    httpHelper.OpenReadAsync();
                                    return;
                                }
                            }
                        }
                    }

                    OnCompleted(httpMethod, args);
                };

            if (input == null)
            {
                httpHelper.OpenReadAsync();
            }
            else
            {
                // we have a request body so write
                httpHelper.OpenWriteCompleted +=
                    (o, e) =>
                    {
                        MsnApiEventArgs args;
                        if (e.Cancelled)
                        {
                            input.Dispose();
                            args = new MsnApiEventArgs(e.Error, true, userState, null);
                        }
                        else if (e.Error == null)
                        {
                            try
                            {
                                using (var stream = e.Result)
                                {
                                    // write input to requestStream
                                    var buffer = new byte[BufferSize];
                                    int nread;

                                    if (notifyUploadProgressChanged)
                                    {
                                        long totalBytesToSend = input.Length;
                                        long bytesSent = 0;

                                        while ((nread = input.Read(buffer, 0, buffer.Length)) != 0)
                                        {
                                            stream.Write(buffer, 0, nread);
                                            stream.Flush();

                                            // notify upload progress changed
                                            bytesSent += nread;
                                            OnUploadProgressChanged(new MsnUploadProgressChangedEventArgs(0, 0, bytesSent, totalBytesToSend, ((int)(bytesSent * 100 / totalBytesToSend)), userState));
                                        }
                                    }
                                    else
                                    {
                                        while ((nread = input.Read(buffer, 0, buffer.Length)) != 0)
                                        {
                                            stream.Write(buffer, 0, nread);
                                            stream.Flush();
                                        }
                                    }
                                }

                                httpHelper.OpenReadAsync();
                                return;
                            }
                            catch (Exception ex)
                            {
                                args = new MsnApiEventArgs(ex, httpHelper.HttpWebRequest.IsCancelled, userState, null);
                            }
                            finally
                            {
                                input.Dispose();
                            }
                        }
                        else
                        {
                            input.Dispose();
                            var webExceptionWrapper = e.Error as WebExceptionWrapper;
                            if (webExceptionWrapper != null)
                            {
                                var ex = webExceptionWrapper;
                                if (ex.GetResponse() != null)
                                {
                                    httpHelper.OpenReadAsync();
                                    return;
                                }
                            }

                            args = new MsnApiEventArgs(e.Error, false, userState, null);
                        }

                        OnCompleted(httpMethod, args);
                    };

                httpHelper.OpenWriteAsync();
            }
        }

        #region Events

        /// <summary>
        /// Raise OnGetCompleted event handler.
        /// </summary>
        /// <param name="args">The <see cref="MsnApiEventArgs"/>.</param>
#if FLUENTHTTP_CORE_TPL
        [Obsolete]
        [SuppressMessage("Microsoft.Design", "CA1041:ProvideObsoleteAttributeMessage")]
#endif
        protected virtual void OnGetCompleted(MsnApiEventArgs args)
        {
            if (GetCompleted != null)
                GetCompleted(this, args);
        }

        /// <summary>
        /// Raise OnPostCompleted event handler.
        /// </summary>
        /// <param name="args">The <see cref="MsnApiEventArgs"/>.</param>
#if FLUENTHTTP_CORE_TPL
        [Obsolete]
        [SuppressMessage("Microsoft.Design", "CA1041:ProvideObsoleteAttributeMessage")]
#endif
        protected virtual void OnPostCompleted(MsnApiEventArgs args)
        {
            if (PostCompleted != null)
                PostCompleted(this, args);
        }

        /// <summary>
        /// Raise OnDeletedCompleted event handler.
        /// </summary>
        /// <param name="args">The <see cref="MsnApiEventArgs"/>.</param>
#if FLUENTHTTP_CORE_TPL
        [Obsolete]
        [SuppressMessage("Microsoft.Design", "CA1041:ProvideObsoleteAttributeMessage")]
#endif
        protected virtual void OnDeleteCompleted(MsnApiEventArgs args)
        {
            if (DeleteCompleted != null)
                DeleteCompleted(this, args);
        }

        /// <summary>
        /// Raise OnUploadProgressCompleted event handler.
        /// </summary>
        /// <param name="args">The <see cref="MsnApiEventArgs"/>.</param>
#if FLUENTHTTP_CORE_TPL
        [Obsolete]
        [SuppressMessage("Microsoft.Design", "CA1041:ProvideObsoleteAttributeMessage")]
#endif
        protected void OnUploadProgressChanged(MsnUploadProgressChangedEventArgs args)
        {
            if (UploadProgressChanged != null)
                UploadProgressChanged(this, args);
        }

#if FLUENTHTTP_CORE_TPL
        [Obsolete]
        [SuppressMessage("Microsoft.Design", "CA1041:ProvideObsoleteAttributeMessage")]
#endif
        private void OnCompleted(HttpMethod httpMethod, MsnApiEventArgs args)
        {
            switch (httpMethod)
            {
                case HttpMethod.Get:
                    OnGetCompleted(args);
                    break;
                case HttpMethod.Post:
                    OnPostCompleted(args);
                    break;
                case HttpMethod.Delete:
                    OnDeleteCompleted(args);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("httpMethod");
            }
        }

        #endregion

        /// <summary>
        /// Makes an asynchronous GET request to the Msn server.
        /// </summary>
        /// <param name="path">The resource path or the resource url.</param>
        /// <returns>The json result.</returns>
#if FLUENTHTTP_CORE_TPL
        [Obsolete("Use GetTaskAsync instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public virtual void GetAsync(string path)
        {
            GetAsync(path, null, null);
        }

        /// <summary>
        /// Makes an asynchronous GET request to the Msn server.
        /// </summary>
        /// <param name="parameters">The parameters</param>
        /// <returns>The json result.</returns>
#if FLUENTHTTP_CORE_TPL
        [Obsolete("Use GetTaskAsync instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public virtual void GetAsync(object parameters)
        {
            GetAsync(null, parameters, null);
        }
        /// <summary>
        /// Makes an asynchronous GET request to the Msn server.
        /// </summary>
        /// <param name="path">The resource path or the resource url.</param>
        /// <param name="parameters">The parameters</param>
        /// <returns>The json result.</returns>
#if FLUENTHTTP_CORE_TPL
        [Obsolete("Use GetTaskAsync instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public virtual void GetAsync(string path, object parameters)
        {
            GetAsync(path, parameters, null);
        }

        /// <summary>
        /// Makes an asynchronous GET request to the Msn server.
        /// </summary>
        /// <param name="path">The resource path or the resource url.</param>
        /// <param name="parameters">The parameters</param>
        /// <param name="userState">The user state.</param>
        /// <returns>The json result.</returns>
#if FLUENTHTTP_CORE_TPL
        [Obsolete("Use GetTaskAsync instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public virtual void GetAsync(string path, object parameters, object userState)
        {
            ApiAsync(HttpMethod.Get, path, parameters, null, userState);
        }

        /// <summary>
        /// Makes an asynchronous POST request to the Msn server.
        /// </summary>
        /// <param name="parameters">The parameters</param>
        /// <returns>The json result.</returns>
#if FLUENTHTTP_CORE_TPL
        [Obsolete("Use PostTaskAsync instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public virtual void PostAsync(object parameters)
        {
            PostAsync(null, parameters, null);
        }

        /// <summary>
        /// Makes an asynchronous POST request to the Msn server.
        /// </summary>
        /// <param name="path">The resource path or the resource url.</param>
        /// <param name="parameters">The parameters</param>
        /// <returns>The json result.</returns>
#if FLUENTHTTP_CORE_TPL
        [Obsolete("Use PostTaskAsync instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public virtual void PostAsync(string path, object parameters)
        {
            PostAsync(path, parameters, null);
        }

        /// <summary>
        /// Makes an asynchronous POST request to the Msn server.
        /// </summary>
        /// <param name="path">The resource path or the resource url.</param>
        /// <param name="parameters">The parameters</param>
        /// <param name="userState">The user state.</param>
        /// <returns>The json result.</returns>
#if FLUENTHTTP_CORE_TPL
        [Obsolete("Use PostTaskAsync instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public virtual void PostAsync(string path, object parameters, object userState)
        {
            ApiAsync(HttpMethod.Post, path, parameters, null, userState);
        }

        /// <summary>
        /// Makes an asynchronous DELETE request to the Msn server.
        /// </summary>
        /// <param name="path">The resource path or the resource url.</param>
        /// <returns>The json result.</returns>
#if FLUENTHTTP_CORE_TPL
        [Obsolete("Use DeleteTaskAsync instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public virtual void DeleteAsync(string path)
        {
            DeleteAsync(path, null, null);
        }

        /// <summary>
        /// Makes an asynchronous DELETE request to the Msn server.
        /// </summary>
        /// <param name="path">The resource path or the resource url.</param>
        /// <param name="parameters">The parameters</param>
        /// <param name="userState">The user state.</param>
        /// <returns>The json result.</returns>
#if FLUENTHTTP_CORE_TPL
        [Obsolete("Use DeleteTaskAsync instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public virtual void DeleteAsync(string path, object parameters, object userState)
        {
            ApiAsync(HttpMethod.Delete, path, parameters, null, userState);
        }
    }
}