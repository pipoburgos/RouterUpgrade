using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RouterUpgrade
{
    public class AgentBase
    {
        public string BaseAddress { get; }

        public List<Cookie> Cookies { get; }

        public AgentBase(string baseAddress, Cookie cookie = default)
        {
            BaseAddress = baseAddress;
            Cookies = new List<Cookie>();

            if (cookie != default)
                Cookies.Add(cookie);
        }

        public async Task<TResult> GetAsync<TResult>(string method)
        {
            return await CreateClientAsync<TResult>(
                async (client, uri) => await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, method)), "GET",
                new Dictionary<string, object>(), 5);
        }

        /// <summary>
        /// Login
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="method"></param>
        /// <param name="parameters"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<TResult> PostFormUrlEncodedAsync<TResult>(string method,
            Dictionary<string, string> parameters, int timeout = 60)
        {
            return await CreateClientAsync<TResult>(
                async (client, uri) =>
                    await client.SendAsync(
                        new HttpRequestMessage(HttpMethod.Post, method)
                        {
                            Content = new FormUrlEncodedContent(parameters)
                        }), method, null, timeout);
        }

        /// <summary>
        /// Subida fichero
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="method"></param>
        /// <param name="filename"></param>
        /// <param name="fileBytes"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<TResult> PostFormUrlEncodedAsync<TResult>(string method, string filename, byte[] fileBytes, int timeout = 300)
        {
            return await CreateClientAsync<TResult>(
                async (client, uri) =>
                {
                    var requestContent = new MultipartFormDataContent("----WebKitFormBoundaryL2U9KCk1sNoWIaAd");

                    var imageContent = new ByteArrayContent(fileBytes);
                    requestContent.Add(imageContent, filename, filename);

                    using (var x = new HttpRequestMessage(HttpMethod.Post, new Uri(BaseAddress + uri))
                    {
                        Version = HttpVersion.Version11,
                        Content = requestContent
                    })
                    {
                        return await client.SendAsync(x);
                    }

                }, method, null, timeout);
        }

        private async Task<TResult> CreateClientAsync<TResult>(
            Func<HttpClient, string, Task<HttpResponseMessage>> callAsync, string method,
            Dictionary<string, object> parameters, int timeout)
        {
            var requestUri = GetRequestUri(method, parameters);

            var cookieContainer = new CookieContainer();
            Cookies.ForEach(cookie =>
            {
                Debug.WriteLine($"{cookie.Name}= {cookie.Value}");
                cookieContainer.Add(cookie);
            });

            var handler = new HttpClientHandler { CookieContainer = cookieContainer };
            using (var client = new HttpClient(handler) { BaseAddress = new Uri(BaseAddress) })
            {
                client.Timeout = TimeSpan.FromSeconds(timeout);

                client.DefaultRequestHeaders.Connection.Add("keep-alive");
                client.DefaultRequestHeaders.ConnectionClose = false;
                if (method == "/te_uploadinfo.asp")
                    client.DefaultRequestHeaders.Referrer =
                        new Uri("http://192.168.1.1/te_actualizaciones_firmware.asp");

                if (method == "/te_wifi.asp")
                    client.DefaultRequestHeaders.Referrer =
                        new Uri("http://192.168.1.1/cgi-bin/te_acceso_router.cgi");

                client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");

                var response = await callAsync(client, requestUri);

                //if (method == "/cgi-bin/te_acceso_router.cgi")
                //{
                //    var stringResult = await response.Content.ReadAsStringAsync();

                //    if (!string.IsNullOrWhiteSpace(stringResult))
                //        Debug.WriteLine(stringResult);
                //}

                if (method == "/te_wifi.asp")
                {
                    if (response.StatusCode == HttpStatusCode.Found)
                        MessageBox.Show("Error login");
                }

                Debug.WriteLine(await response.Content.ReadAsStringAsync());

                var cookieUri = new Uri(BaseAddress + requestUri);

                var listaCookiesRespuesta = cookieContainer
                    .GetCookies(cookieUri)
                    .Cast<Cookie>()
                    .Distinct()
                    .ToList();

                Cookies.Clear();

                foreach (var cookie in listaCookiesRespuesta)
                {
                    Debug.WriteLine($"{cookie.Name}={cookie.Value}");
                    Cookies.Add(new Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain));
                }

                return await ValidateAndDeserialize<TResult>(response);
            }
        }

        //private void ReadCookies(HttpResponseMessage response, string domain)
        //{
        //    foreach (var httpResponseHeader in response.Headers)
        //    {
        //        if (httpResponseHeader.Key != "Set-Cookie")
        //            continue;

        //        var value = httpResponseHeader.Value.ToString();
        //        foreach (var singleCookie in value.Split(','))
        //        {
        //            var match = Regex.Match(singleCookie, "(.+?)=(.+?);");
        //            if (match.Captures.Count == 0)
        //                continue;

        //            Cookies.Add(new Cookie(match.Groups[1].ToString(), match.Groups[2].ToString(), "/", domain));
        //        }
        //    }
        //}

        private static string GetRequestUri(string method, Dictionary<string, object> parameters)
        {
            var requestUri = method.TrimEnd('/') + parameters?.DictionaryToQueryString();
            return requestUri;
        }

        private async Task<TResult> ValidateAndDeserialize<TResult>(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
                return default(TResult);

            var stringResult = await response.Content.ReadAsStringAsync();
            if (typeof(TResult) == typeof(string))
                return (TResult)Convert.ChangeType(stringResult, typeof(TResult));

            return default(TResult);
        }
    }


    public static class UrlUtils
    {
        public static string DictionaryToQueryString(this Dictionary<string, object> parameters)
        {
            var stringBuilder = new StringBuilder("?");
            var flag = true;
            foreach (var str in parameters.Keys.ToArray())
            {
                if (!flag)
                    stringBuilder.Append("&");
                parameters.TryGetValue(str, out var obj);
                stringBuilder.AppendFormat("{0}={1}", Uri.EscapeDataString(str), Uri.EscapeDataString(Convert.ToString(obj)));
                flag = false;
            }
            return !flag ? stringBuilder.ToString() : string.Empty;
        }
    }
}