// ***********************************************************************
// <copyright file="HttpUtils.HttpClient.cs" company="ServiceStack, Inc.">
//     Copyright (c) ServiceStack, Inc. All Rights Reserved.
// </copyright>
// <summary>Fork for YetAnotherForum.NET, Licensed under the Apache License, Version 2.0</summary>
// ***********************************************************************

#if NET6_0_OR_GREATER
#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack
{

    public static partial class HttpUtils
    {
        private class HttpClientFactory
        {
            private readonly Lazy<HttpMessageHandler> lazyHandler;

            internal HttpClientFactory(HttpClientHandler handler) =>
                lazyHandler = new Lazy<HttpMessageHandler>(() => handler, LazyThreadSafetyMode.ExecutionAndPublication);

            public HttpClient CreateClient() => new(lazyHandler.Value, disposeHandler: false);
        }

        // Ok to use HttpClientHandler which now uses SocketsHttpHandler
        // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Net.Http/src/System/Net/Http/HttpClientHandler.cs#L16
        public static HttpClientHandler HttpClientHandler { get; set; } = new()
                                                                              {
                                                                                  UseDefaultCredentials = true,
                                                                                  AutomaticDecompression =
                                                                                      DecompressionMethods.Brotli
                                                                                      | DecompressionMethods.Deflate
                                                                                      | DecompressionMethods.GZip,
                                                                              };

        // This was the least desirable end to this sadness https://github.com/dotnet/aspnetcore/issues/28385
        // Requires <PackageReference Include="Microsoft.Extensions.Http" Version="6.0.0" /> 
        // public static IHttpClientFactory ClientFactory { get; set; } = new ServiceCollection()
        //     .AddHttpClient()
        //     .Configure<HttpClientFactoryOptions>(options => 
        //         options.HttpMessageHandlerBuilderActions.Add(builder => builder.PrimaryHandler = HandlerFactory))
        //     .BuildServiceProvider().GetRequiredService<IHttpClientFactory>();

        // Escape & BYO IHttpClientFactory
        private static HttpClientFactory clientFactory = new(HttpClientHandler);

        public static Func<HttpClient> CreateClient { get; set; } = () => clientFactory.CreateClient();

        public static HttpClient Create() => CreateClient();

        public static string GetJsonFromUrl(
            this string url,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return url.GetStringFromUrl(MimeTypes.Json, requestFilter, responseFilter);
        }

        public static Task<string> GetJsonFromUrlAsync(
            this string url,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return url.GetStringFromUrlAsync(MimeTypes.Json, requestFilter, responseFilter, token: token);
        }

        public static string GetXmlFromUrl(
            this string url,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return url.GetStringFromUrl(MimeTypes.Xml, requestFilter, responseFilter);
        }

        public static Task<string> GetXmlFromUrlAsync(
            this string url,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return url.GetStringFromUrlAsync(MimeTypes.Xml, requestFilter, responseFilter, token: token);
        }

        public static string GetCsvFromUrl(
            this string url,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return url.GetStringFromUrl(MimeTypes.Csv, requestFilter, responseFilter);
        }

        public static Task<string> GetCsvFromUrlAsync(
            this string url,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return url.GetStringFromUrlAsync(MimeTypes.Csv, requestFilter, responseFilter, token: token);
        }

        public static string GetStringFromUrl(
            this string url,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return SendStringToUrl(
                url,
                method: HttpMethods.Get,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static Task<string> GetStringFromUrlAsync(
            this string url,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return SendStringToUrlAsync(
                url,
                method: HttpMethods.Get,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static string PostStringToUrl(
            this string url,
            string? requestBody = null,
            string? contentType = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return SendStringToUrl(
                url,
                method: HttpMethods.Post,
                requestBody: requestBody,
                contentType: contentType,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static Task<string> PostStringToUrlAsync(
            this string url,
            string? requestBody = null,
            string? contentType = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return SendStringToUrlAsync(
                url,
                method: HttpMethods.Post,
                requestBody: requestBody,
                contentType: contentType,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static string PostToUrl(
            this string url,
            string? formData = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return SendStringToUrl(
                url,
                method: HttpMethods.Post,
                contentType: MimeTypes.FormUrlEncoded,
                requestBody: formData,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static Task<string> PostToUrlAsync(
            this string url,
            string? formData = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return SendStringToUrlAsync(
                url,
                method: HttpMethods.Post,
                contentType: MimeTypes.FormUrlEncoded,
                requestBody: formData,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static string PostToUrl(
            this string url,
            object? formData = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            string? postFormData = formData != null ? QueryStringSerializer.SerializeToString(formData) : null;

            return SendStringToUrl(
                url,
                method: HttpMethods.Post,
                contentType: MimeTypes.FormUrlEncoded,
                requestBody: postFormData,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static Task<string> PostToUrlAsync(
            this string url,
            object? formData = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            string? postFormData = formData != null ? QueryStringSerializer.SerializeToString(formData) : null;

            return SendStringToUrlAsync(
                url,
                method: HttpMethods.Post,
                contentType: MimeTypes.FormUrlEncoded,
                requestBody: postFormData,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static string PostJsonToUrl(
            this string url,
            string json,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return SendStringToUrl(
                url,
                method: HttpMethods.Post,
                requestBody: json,
                contentType: MimeTypes.Json,
                accept: MimeTypes.Json,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static Task<string> PostJsonToUrlAsync(
            this string url,
            string json,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return SendStringToUrlAsync(
                url,
                method: HttpMethods.Post,
                requestBody: json,
                contentType: MimeTypes.Json,
                accept: MimeTypes.Json,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static string PostJsonToUrl(
            this string url,
            object data,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return SendStringToUrl(
                url,
                method: HttpMethods.Post,
                requestBody: data.ToJson(),
                contentType: MimeTypes.Json,
                accept: MimeTypes.Json,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static Task<string> PostJsonToUrlAsync(
            this string url,
            object data,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return SendStringToUrlAsync(
                url,
                method: HttpMethods.Post,
                requestBody: data.ToJson(),
                contentType: MimeTypes.Json,
                accept: MimeTypes.Json,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static string PostXmlToUrl(
            this string url,
            string xml,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return SendStringToUrl(
                url,
                method: HttpMethods.Post,
                requestBody: xml,
                contentType: MimeTypes.Xml,
                accept: MimeTypes.Xml,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static Task<string> PostXmlToUrlAsync(
            this string url,
            string xml,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return SendStringToUrlAsync(
                url,
                method: HttpMethods.Post,
                requestBody: xml,
                contentType: MimeTypes.Xml,
                accept: MimeTypes.Xml,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static string PostCsvToUrl(
            this string url,
            string csv,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return SendStringToUrl(
                url,
                method: HttpMethods.Post,
                requestBody: csv,
                contentType: MimeTypes.Csv,
                accept: MimeTypes.Csv,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static Task<string> PostCsvToUrlAsync(
            this string url,
            string csv,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return SendStringToUrlAsync(
                url,
                method: HttpMethods.Post,
                requestBody: csv,
                contentType: MimeTypes.Csv,
                accept: MimeTypes.Csv,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static string PutStringToUrl(
            this string url,
            string? requestBody = null,
            string? contentType = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return SendStringToUrl(
                url,
                method: HttpMethods.Put,
                requestBody: requestBody,
                contentType: contentType,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static Task<string> PutStringToUrlAsync(
            this string url,
            string? requestBody = null,
            string? contentType = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return SendStringToUrlAsync(
                url,
                method: HttpMethods.Put,
                requestBody: requestBody,
                contentType: contentType,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static string PutToUrl(
            this string url,
            string? formData = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return SendStringToUrl(
                url,
                method: HttpMethods.Put,
                contentType: MimeTypes.FormUrlEncoded,
                requestBody: formData,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static Task<string> PutToUrlAsync(
            this string url,
            string? formData = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return SendStringToUrlAsync(
                url,
                method: HttpMethods.Put,
                contentType: MimeTypes.FormUrlEncoded,
                requestBody: formData,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static string PutToUrl(
            this string url,
            object? formData = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            string? postFormData = formData != null ? QueryStringSerializer.SerializeToString(formData) : null;

            return SendStringToUrl(
                url,
                method: HttpMethods.Put,
                contentType: MimeTypes.FormUrlEncoded,
                requestBody: postFormData,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static Task<string> PutToUrlAsync(
            this string url,
            object? formData = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            string? postFormData = formData != null ? QueryStringSerializer.SerializeToString(formData) : null;

            return SendStringToUrlAsync(
                url,
                method: HttpMethods.Put,
                contentType: MimeTypes.FormUrlEncoded,
                requestBody: postFormData,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static string PutJsonToUrl(
            this string url,
            string json,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return SendStringToUrl(
                url,
                method: HttpMethods.Put,
                requestBody: json,
                contentType: MimeTypes.Json,
                accept: MimeTypes.Json,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static Task<string> PutJsonToUrlAsync(
            this string url,
            string json,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return SendStringToUrlAsync(
                url,
                method: HttpMethods.Put,
                requestBody: json,
                contentType: MimeTypes.Json,
                accept: MimeTypes.Json,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static string PutJsonToUrl(
            this string url,
            object data,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return SendStringToUrl(
                url,
                method: HttpMethods.Put,
                requestBody: data.ToJson(),
                contentType: MimeTypes.Json,
                accept: MimeTypes.Json,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static Task<string> PutJsonToUrlAsync(
            this string url,
            object data,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return SendStringToUrlAsync(
                url,
                method: HttpMethods.Put,
                requestBody: data.ToJson(),
                contentType: MimeTypes.Json,
                accept: MimeTypes.Json,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static string PutXmlToUrl(
            this string url,
            string xml,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return SendStringToUrl(
                url,
                method: HttpMethods.Put,
                requestBody: xml,
                contentType: MimeTypes.Xml,
                accept: MimeTypes.Xml,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static Task<string> PutXmlToUrlAsync(
            this string url,
            string xml,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return SendStringToUrlAsync(
                url,
                method: HttpMethods.Put,
                requestBody: xml,
                contentType: MimeTypes.Xml,
                accept: MimeTypes.Xml,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static string PutCsvToUrl(
            this string url,
            string csv,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return SendStringToUrl(
                url,
                method: HttpMethods.Put,
                requestBody: csv,
                contentType: MimeTypes.Csv,
                accept: MimeTypes.Csv,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static Task<string> PutCsvToUrlAsync(
            this string url,
            string csv,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return SendStringToUrlAsync(
                url,
                method: HttpMethods.Put,
                requestBody: csv,
                contentType: MimeTypes.Csv,
                accept: MimeTypes.Csv,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static string PatchStringToUrl(
            this string url,
            string? requestBody = null,
            string? contentType = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return SendStringToUrl(
                url,
                method: HttpMethods.Patch,
                requestBody: requestBody,
                contentType: contentType,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static Task<string> PatchStringToUrlAsync(
            this string url,
            string? requestBody = null,
            string? contentType = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return SendStringToUrlAsync(
                url,
                method: HttpMethods.Patch,
                requestBody: requestBody,
                contentType: contentType,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static string PatchToUrl(
            this string url,
            string? formData = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return SendStringToUrl(
                url,
                method: HttpMethods.Patch,
                contentType: MimeTypes.FormUrlEncoded,
                requestBody: formData,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static Task<string> PatchToUrlAsync(
            this string url,
            string? formData = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return SendStringToUrlAsync(
                url,
                method: HttpMethods.Patch,
                contentType: MimeTypes.FormUrlEncoded,
                requestBody: formData,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static string PatchToUrl(
            this string url,
            object? formData = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            string? postFormData = formData != null ? QueryStringSerializer.SerializeToString(formData) : null;

            return SendStringToUrl(
                url,
                method: HttpMethods.Patch,
                contentType: MimeTypes.FormUrlEncoded,
                requestBody: postFormData,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static Task<string> PatchToUrlAsync(
            this string url,
            object? formData = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            string? postFormData = formData != null ? QueryStringSerializer.SerializeToString(formData) : null;

            return SendStringToUrlAsync(
                url,
                method: HttpMethods.Patch,
                contentType: MimeTypes.FormUrlEncoded,
                requestBody: postFormData,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static string PatchJsonToUrl(
            this string url,
            string json,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return SendStringToUrl(
                url,
                method: HttpMethods.Patch,
                requestBody: json,
                contentType: MimeTypes.Json,
                accept: MimeTypes.Json,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static Task<string> PatchJsonToUrlAsync(
            this string url,
            string json,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return SendStringToUrlAsync(
                url,
                method: HttpMethods.Patch,
                requestBody: json,
                contentType: MimeTypes.Json,
                accept: MimeTypes.Json,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static string PatchJsonToUrl(
            this string url,
            object data,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return SendStringToUrl(
                url,
                method: HttpMethods.Patch,
                requestBody: data.ToJson(),
                contentType: MimeTypes.Json,
                accept: MimeTypes.Json,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static Task<string> PatchJsonToUrlAsync(
            this string url,
            object data,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return SendStringToUrlAsync(
                url,
                method: HttpMethods.Patch,
                requestBody: data.ToJson(),
                contentType: MimeTypes.Json,
                accept: MimeTypes.Json,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static string DeleteFromUrl(
            this string url,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return SendStringToUrl(
                url,
                method: HttpMethods.Delete,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static Task<string> DeleteFromUrlAsync(
            this string url,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return SendStringToUrlAsync(
                url,
                method: HttpMethods.Delete,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static string OptionsFromUrl(
            this string url,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return SendStringToUrl(
                url,
                method: HttpMethods.Options,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static Task<string> OptionsFromUrlAsync(
            this string url,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return SendStringToUrlAsync(
                url,
                method: HttpMethods.Options,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static string HeadFromUrl(
            this string url,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return SendStringToUrl(
                url,
                method: HttpMethods.Head,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static Task<string> HeadFromUrlAsync(
            this string url,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return SendStringToUrlAsync(
                url,
                method: HttpMethods.Head,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static string SendStringToUrl(
            this string url,
            string method = HttpMethods.Post,
            string? requestBody = null,
            string? contentType = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return Create().SendStringToUrl(
                url,
                method,
                requestBody,
                contentType,
                accept,
                requestFilter,
                responseFilter);
        }

        public static string SendStringToUrl(
            this HttpClient client,
            string url,
            string method = HttpMethods.Post,
            string? requestBody = null,
            string? contentType = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            var httpReq = new HttpRequestMessage(new HttpMethod(method), url);
            httpReq.Headers.Add(HttpHeaders.Accept, accept);

            if (requestBody != null)
            {
                httpReq.Content = new StringContent(requestBody, UseEncoding);
                if (contentType != null)
                    httpReq.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            }

            requestFilter?.Invoke(httpReq);

            var httpRes = client.Send(httpReq);
            responseFilter?.Invoke(httpRes);
            httpRes.EnsureSuccessStatusCode();
            return httpRes.Content.ReadAsStream().ReadToEnd(UseEncoding);
        }

        public static Task<string> SendStringToUrlAsync(
            this string url,
            string method = HttpMethods.Post,
            string? requestBody = null,
            string? contentType = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return Create().SendStringToUrlAsync(
                url,
                method,
                requestBody,
                contentType,
                accept,
                requestFilter,
                responseFilter,
                token);
        }

        public static async Task<string> SendStringToUrlAsync(
            this HttpClient client,
            string url,
            string method = HttpMethods.Post,
            string? requestBody = null,
            string? contentType = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            var httpReq = new HttpRequestMessage(new HttpMethod(method), url);
            httpReq.Headers.Add(HttpHeaders.Accept, accept);

            if (requestBody != null)
            {
                httpReq.Content = new StringContent(requestBody, UseEncoding);
                if (contentType != null)
                    httpReq.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            }

            requestFilter?.Invoke(httpReq);

            var httpRes = await client.SendAsync(httpReq, token).ConfigAwait();
            responseFilter?.Invoke(httpRes);
            httpRes.EnsureSuccessStatusCode();
            return await httpRes.Content.ReadAsStringAsync(token).ConfigAwait();
        }

        public static byte[] GetBytesFromUrl(
            this string url,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return url.SendBytesToUrl(accept: accept, requestFilter: requestFilter, responseFilter: responseFilter);
        }

        public static Task<byte[]> GetBytesFromUrlAsync(
            this string url,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return url.SendBytesToUrlAsync(
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static byte[] PostBytesToUrl(
            this string url,
            byte[]? requestBody = null,
            string? contentType = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return SendBytesToUrl(
                url,
                method: HttpMethods.Post,
                contentType: contentType,
                requestBody: requestBody,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static Task<byte[]> PostBytesToUrlAsync(
            this string url,
            byte[]? requestBody = null,
            string? contentType = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return SendBytesToUrlAsync(
                url,
                method: HttpMethods.Post,
                contentType: contentType,
                requestBody: requestBody,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static byte[] PutBytesToUrl(
            this string url,
            byte[]? requestBody = null,
            string? contentType = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return SendBytesToUrl(
                url,
                method: HttpMethods.Put,
                contentType: contentType,
                requestBody: requestBody,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static Task<byte[]> PutBytesToUrlAsync(
            this string url,
            byte[]? requestBody = null,
            string? contentType = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return SendBytesToUrlAsync(
                url,
                method: HttpMethods.Put,
                contentType: contentType,
                requestBody: requestBody,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static byte[] SendBytesToUrl(
            this string url,
            string method = HttpMethods.Post,
            byte[]? requestBody = null,
            string? contentType = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return Create().SendBytesToUrl(
                url,
                method,
                requestBody,
                contentType,
                accept,
                requestFilter,
                responseFilter);
        }

        public static byte[] SendBytesToUrl(
            this HttpClient client,
            string url,
            string method = HttpMethods.Post,
            byte[]? requestBody = null,
            string? contentType = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            var httpReq = new HttpRequestMessage(new HttpMethod(method), url);
            httpReq.Headers.Add(HttpHeaders.Accept, accept);

            if (requestBody != null)
            {
                httpReq.Content = new ReadOnlyMemoryContent(requestBody);
                if (contentType != null)
                    httpReq.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            }

            requestFilter?.Invoke(httpReq);

            var httpRes = client.Send(httpReq);
            responseFilter?.Invoke(httpRes);
            httpRes.EnsureSuccessStatusCode();
            return httpRes.Content.ReadAsStream().ReadFully();
        }

        public static Task<byte[]> SendBytesToUrlAsync(
            this string url,
            string method = HttpMethods.Post,
            byte[]? requestBody = null,
            string? contentType = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return Create().SendBytesToUrlAsync(
                url,
                method,
                requestBody,
                contentType,
                accept,
                requestFilter,
                responseFilter,
                token);
        }

        public static async Task<byte[]> SendBytesToUrlAsync(
            this HttpClient client,
            string url,
            string method = HttpMethods.Post,
            byte[]? requestBody = null,
            string? contentType = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            var httpReq = new HttpRequestMessage(new HttpMethod(method), url);
            httpReq.Headers.Add(HttpHeaders.Accept, accept);

            if (requestBody != null)
            {
                httpReq.Content = new ReadOnlyMemoryContent(requestBody);
                if (contentType != null)
                    httpReq.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            }

            requestFilter?.Invoke(httpReq);

            var httpRes = await client.SendAsync(httpReq, token).ConfigAwait();
            responseFilter?.Invoke(httpRes);
            httpRes.EnsureSuccessStatusCode();
            return await (await httpRes.Content.ReadAsStreamAsync(token).ConfigAwait()).ReadFullyAsync(token)
                       .ConfigAwait();
        }

        public static Stream GetStreamFromUrl(
            this string url,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return url.SendStreamToUrl(accept: accept, requestFilter: requestFilter, responseFilter: responseFilter);
        }

        public static Task<Stream> GetStreamFromUrlAsync(
            this string url,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return url.SendStreamToUrlAsync(
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static Stream PostStreamToUrl(
            this string url,
            Stream? requestBody = null,
            string? contentType = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return SendStreamToUrl(
                url,
                method: HttpMethods.Post,
                contentType: contentType,
                requestBody: requestBody,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static Task<Stream> PostStreamToUrlAsync(
            this string url,
            Stream? requestBody = null,
            string? contentType = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return SendStreamToUrlAsync(
                url,
                method: HttpMethods.Post,
                contentType: contentType,
                requestBody: requestBody,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static Stream PutStreamToUrl(
            this string url,
            Stream? requestBody = null,
            string? contentType = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return SendStreamToUrl(
                url,
                method: HttpMethods.Put,
                contentType: contentType,
                requestBody: requestBody,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static Task<Stream> PutStreamToUrlAsync(
            this string url,
            Stream? requestBody = null,
            string? contentType = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return SendStreamToUrlAsync(
                url,
                method: HttpMethods.Put,
                contentType: contentType,
                requestBody: requestBody,
                accept: accept,
                requestFilter: requestFilter,
                responseFilter: responseFilter,
                token: token);
        }

        public static Stream SendStreamToUrl(
            this string url,
            string method = HttpMethods.Post,
            Stream? requestBody = null,
            string? contentType = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return Create().SendStreamToUrl(
                url,
                method,
                requestBody,
                contentType,
                accept,
                requestFilter,
                responseFilter);
        }

        public static Stream SendStreamToUrl(
            this HttpClient client,
            string url,
            string method = HttpMethods.Post,
            Stream? requestBody = null,
            string? contentType = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            var httpReq = new HttpRequestMessage(new HttpMethod(method), url);
            httpReq.Headers.Add(HttpHeaders.Accept, accept);

            if (requestBody != null)
            {
                httpReq.Content = new StreamContent(requestBody);
                if (contentType != null)
                    httpReq.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            }

            requestFilter?.Invoke(httpReq);

            var httpRes = client.Send(httpReq);
            responseFilter?.Invoke(httpRes);
            httpRes.EnsureSuccessStatusCode();
            return httpRes.Content.ReadAsStream();
        }

        public static Task<Stream> SendStreamToUrlAsync(
            this string url,
            string method = HttpMethods.Post,
            Stream? requestBody = null,
            string? contentType = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return Create().SendStreamToUrlAsync(
                url,
                method,
                requestBody,
                contentType,
                accept,
                requestFilter,
                responseFilter,
                token);
        }

        public static async Task<Stream> SendStreamToUrlAsync(
            this HttpClient client,
            string url,
            string method = HttpMethods.Post,
            Stream? requestBody = null,
            string? contentType = null,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            var httpReq = new HttpRequestMessage(new HttpMethod(method), url);
            httpReq.Headers.Add(HttpHeaders.Accept, accept);

            if (requestBody != null)
            {
                httpReq.Content = new StreamContent(requestBody);
                if (contentType != null)
                    httpReq.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            }

            requestFilter?.Invoke(httpReq);

            var httpRes = await client.SendAsync(httpReq, token).ConfigAwait();
            responseFilter?.Invoke(httpRes);
            httpRes.EnsureSuccessStatusCode();
            return await httpRes.Content.ReadAsStreamAsync(token).ConfigAwait();
        }

        public static HttpStatusCode? GetResponseStatus(this string url)
        {
            try
            {
                var client = Create();
                var httpReq = new HttpRequestMessage(new HttpMethod(HttpMethods.Get), url);
                httpReq.Headers.Add(HttpHeaders.Accept, "*/*");
                var httpRes = client.Send(httpReq);
                return httpRes.StatusCode;
            }
            catch (Exception ex)
            {
                return ex.GetStatus();
            }
        }

        public static HttpResponseMessage? GetErrorResponse(this string url)
        {
            var client = Create();
            var httpReq = new HttpRequestMessage(new HttpMethod(HttpMethods.Get), url);
            httpReq.Headers.Add(HttpHeaders.Accept, "*/*");
            var httpRes = client.Send(httpReq);
            return httpRes.IsSuccessStatusCode ? null : httpRes;
        }

        public static async Task<HttpResponseMessage?> GetErrorResponseAsync(
            this string url,
            CancellationToken token = default)
        {
            var client = Create();
            var httpReq = new HttpRequestMessage(new HttpMethod(HttpMethods.Get), url);
            httpReq.Headers.Add(HttpHeaders.Accept, "*/*");
            var httpRes = await client.SendAsync(httpReq, token).ConfigAwait();
            return httpRes.IsSuccessStatusCode ? null : httpRes;
        }

        public static string ReadToEnd(this HttpResponseMessage webRes)
        {
            using var stream = webRes.Content.ReadAsStream();
            return stream.ReadToEnd(UseEncoding);
        }

        public static Task<string> ReadToEndAsync(this HttpResponseMessage webRes)
        {
            using var stream = webRes.Content.ReadAsStream();
            return stream.ReadToEndAsync(UseEncoding);
        }

        public static IEnumerable<string> ReadLines(this HttpResponseMessage webRes)
        {
            using var stream = webRes.Content.ReadAsStream();
            using var reader = new StreamReader(stream, UseEncoding, true, 1024, leaveOpen: true);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }

        public static HttpResponseMessage UploadFile(
            this HttpRequestMessage httpReq,
            Stream fileStream,
            string fileName,
            string? mimeType = null,
            string accept = "*/*",
            string method = HttpMethods.Post,
            string field = "file",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return Create().UploadFile(
                httpReq,
                fileStream,
                fileName,
                mimeType,
                accept,
                method,
                field,
                requestFilter,
                responseFilter);
        }


        public static HttpResponseMessage UploadFile(
            this HttpClient client,
            HttpRequestMessage httpReq,
            Stream fileStream,
            string fileName,
            string? mimeType = null,
            string accept = "*/*",
            string method = HttpMethods.Post,
            string field = "file",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            if (httpReq.RequestUri == null)
                throw new ArgumentException(nameof(httpReq.RequestUri));

            httpReq.Method = new HttpMethod(method);
            httpReq.Headers.Add(HttpHeaders.Accept, accept);
            requestFilter?.Invoke(httpReq);

            using var content = new MultipartFormDataContent();
            var fileBytes = fileStream.ReadFully();
            using var fileContent = new ByteArrayContent(fileBytes, 0, fileBytes.Length);
            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                                                         {
                                                             Name = "file", FileName = fileName
                                                         };
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(mimeType ?? MimeTypes.GetMimeType(fileName));
            content.Add(fileContent, "file", fileName);

            var httpRes = client.Send(httpReq);
            responseFilter?.Invoke(httpRes);
            httpRes.EnsureSuccessStatusCode();
            return httpRes;
        }

        public static Task<HttpResponseMessage> UploadFileAsync(
            this HttpRequestMessage httpReq,
            Stream fileStream,
            string fileName,
            string? mimeType = null,
            string accept = "*/*",
            string method = HttpMethods.Post,
            string field = "file",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            return Create().UploadFileAsync(
                httpReq,
                fileStream,
                fileName,
                mimeType,
                accept,
                method,
                field,
                requestFilter,
                responseFilter,
                token);
        }

        public static async Task<HttpResponseMessage> UploadFileAsync(
            this HttpClient client,
            HttpRequestMessage httpReq,
            Stream fileStream,
            string fileName,
            string? mimeType = null,
            string accept = "*/*",
            string method = HttpMethods.Post,
            string field = "file",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            if (httpReq.RequestUri == null)
                throw new ArgumentException(nameof(httpReq.RequestUri));

            httpReq.Method = new HttpMethod(method);
            httpReq.Headers.Add(HttpHeaders.Accept, accept);
            requestFilter?.Invoke(httpReq);

            using var content = new MultipartFormDataContent();
            var fileBytes = await fileStream.ReadFullyAsync(token).ConfigAwait();
            using var fileContent = new ByteArrayContent(fileBytes, 0, fileBytes.Length);
            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                                                         {
                                                             Name = "file", FileName = fileName
                                                         };
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(mimeType ?? MimeTypes.GetMimeType(fileName));
            content.Add(fileContent, "file", fileName);

            var httpRes = await client.SendAsync(httpReq, token).ConfigAwait();
            responseFilter?.Invoke(httpRes);
            httpRes.EnsureSuccessStatusCode();
            return httpRes;
        }

        public static void UploadFile(this HttpRequestMessage httpReq, Stream fileStream, string fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));
            var mimeType = MimeTypes.GetMimeType(fileName);
            if (mimeType == null)
                throw new ArgumentException("Mime-type not found for file: " + fileName);

            UploadFile(httpReq, fileStream, fileName, mimeType);
        }

        public static async Task UploadFileAsync(
            this HttpRequestMessage webRequest,
            Stream fileStream,
            string fileName,
            CancellationToken token = default)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));
            var mimeType = MimeTypes.GetMimeType(fileName);
            if (mimeType == null)
                throw new ArgumentException("Mime-type not found for file: " + fileName);

            await UploadFileAsync(webRequest, fileStream, fileName, mimeType, token: token).ConfigAwait();
        }

        public static string PostXmlToUrl(
            this string url,
            object data,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return SendStringToUrl(
                url,
                method: HttpMethods.Post,
                requestBody: data.ToXml(),
                contentType: MimeTypes.Xml,
                accept: MimeTypes.Xml,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static string PostCsvToUrl(
            this string url,
            object data,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return SendStringToUrl(
                url,
                method: HttpMethods.Post,
                requestBody: data.ToCsv(),
                contentType: MimeTypes.Csv,
                accept: MimeTypes.Csv,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static string PutXmlToUrl(
            this string url,
            object data,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return SendStringToUrl(
                url,
                method: HttpMethods.Put,
                requestBody: data.ToXml(),
                contentType: MimeTypes.Xml,
                accept: MimeTypes.Xml,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static string PutCsvToUrl(
            this string url,
            object data,
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            return SendStringToUrl(
                url,
                method: HttpMethods.Put,
                requestBody: data.ToCsv(),
                contentType: MimeTypes.Csv,
                accept: MimeTypes.Csv,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static HttpResponseMessage PostFileToUrl(
            this string url,
            FileInfo uploadFileInfo,
            string uploadFileMimeType,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            var webReq = new HttpRequestMessage(HttpMethod.Post, url);
            using var fileStream = uploadFileInfo.OpenRead();
            var fileName = uploadFileInfo.Name;

            return webReq.UploadFile(
                fileStream,
                fileName,
                uploadFileMimeType,
                accept: accept,
                method: HttpMethods.Post,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static async Task<HttpResponseMessage> PostFileToUrlAsync(
            this string url,
            FileInfo uploadFileInfo,
            string uploadFileMimeType,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            var webReq = new HttpRequestMessage(HttpMethod.Post, url);
            await using var fileStream = uploadFileInfo.OpenRead();
            var fileName = uploadFileInfo.Name;

            return await webReq.UploadFileAsync(
                       fileStream,
                       fileName,
                       uploadFileMimeType,
                       accept: accept,
                       method: HttpMethods.Post,
                       requestFilter: requestFilter,
                       responseFilter: responseFilter,
                       token: token).ConfigAwait();
        }

        public static HttpResponseMessage PutFileToUrl(
            this string url,
            FileInfo uploadFileInfo,
            string uploadFileMimeType,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null)
        {
            var webReq = new HttpRequestMessage(HttpMethod.Put, url);
            using var fileStream = uploadFileInfo.OpenRead();
            var fileName = uploadFileInfo.Name;

            return webReq.UploadFile(
                fileStream,
                fileName,
                uploadFileMimeType,
                accept: accept,
                method: HttpMethods.Post,
                requestFilter: requestFilter,
                responseFilter: responseFilter);
        }

        public static async Task<HttpResponseMessage> PutFileToUrlAsync(
            this string url,
            FileInfo uploadFileInfo,
            string uploadFileMimeType,
            string accept = "*/*",
            Action<HttpRequestMessage>? requestFilter = null,
            Action<HttpResponseMessage>? responseFilter = null,
            CancellationToken token = default)
        {
            var webReq = new HttpRequestMessage(HttpMethod.Put, url);
            await using var fileStream = uploadFileInfo.OpenRead();
            var fileName = uploadFileInfo.Name;

            return await webReq.UploadFileAsync(
                       fileStream,
                       fileName,
                       uploadFileMimeType,
                       accept: accept,
                       method: HttpMethods.Post,
                       requestFilter: requestFilter,
                       responseFilter: responseFilter,
                       token: token).ConfigAwait();
        }

        public static void AddHeader(this HttpRequestMessage res, string name, string value) =>
            res.WithHeader(name, value);

        public static string? GetHeader(this HttpRequestMessage res, string name) =>
            res.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;

        public static string? GetHeader(this HttpResponseMessage res, string name) =>
            res.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;

        public static HttpRequestMessage WithHeader(this HttpRequestMessage httpReq, string name, string value)
        {
            if (name.Equals(HttpHeaders.Authorization, StringComparison.OrdinalIgnoreCase))
            {
                httpReq.Headers.Authorization =
                    new AuthenticationHeaderValue(value.LeftPart(' '), value.RightPart(' '));
            }
            else if (name.Equals(HttpHeaders.ContentType, StringComparison.OrdinalIgnoreCase))
            {
                if (httpReq.Content == null)
                    throw new NotSupportedException("Can't set ContentType before Content is populated");
                httpReq.Content.Headers.ContentType = new MediaTypeHeaderValue(value);
            }
            else if (name.Equals(HttpHeaders.UserAgent, StringComparison.OrdinalIgnoreCase))
            {
                if (value.IndexOf('/') >= 0)
                {
                    var product = value.LastLeftPart('/');
                    var version = value.LastRightPart('/');
                    httpReq.Headers.UserAgent.Add(new ProductInfoHeaderValue(product, version));
                }
                else
                {
                    // Avoid format exceptions
                    var commentFmt = value[0] == '(' && value[^1] == ')' ? value : $"({value})";
                    httpReq.Headers.UserAgent.Add(new ProductInfoHeaderValue(commentFmt));
                }
            }
            else
            {
                httpReq.Headers.Add(name, value);
            }

            return httpReq;
        }

        public static HttpRequestMessage With(this HttpRequestMessage httpReq, Action<HttpRequestConfig> configure)
        {
            var config = new HttpRequestConfig();
            configure(config);

            config.Headers ??= new Dictionary<string, string>();
            var headers = config.Headers;

            if (config.Accept != null)
                headers[HttpHeaders.Accept] = config.Accept;
            if (config.UserAgent != null)
                headers[HttpHeaders.UserAgent] = config.UserAgent;
            if (config.ContentType != null)
                headers[HttpHeaders.ContentType] = config.ContentType;
            if (config.Authorization != null)
                httpReq.Headers.Authorization = new AuthenticationHeaderValue(
                    config.Authorization.Value.Key,
                    config.Authorization.Value.Value);

            foreach (var entry in headers)
            {
                httpReq.WithHeader(entry.Key, entry.Value);
            }

            return httpReq;
        }

        public static void DownloadFileTo(
            this string downloadUrl,
            string fileName,
            Dictionary<string, string>? headers = null)
        {
            var client = Create();
            var httpReq = new HttpRequestMessage(HttpMethod.Get, downloadUrl).With(
                c =>
                    {
                        c.Accept = "*/*";
                        c.Headers = headers;
                    });

            var httpRes = client.Send(httpReq);
            httpRes.EnsureSuccessStatusCode();

            using var fs = new FileStream(fileName, FileMode.CreateNew);
            var bytes = httpRes.Content.ReadAsStream().ReadFully();
            fs.Write(bytes);
        }
    }
}
#endif