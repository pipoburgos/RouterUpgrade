using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RouterUpgrade
{
    public class AgentBase
    {
        public string BaseAddress { get; }

        public List<Cookie> Cookies { get; }

        public AgentBase(string baseAddress)
        {
            BaseAddress = baseAddress;
            Cookies = new List<Cookie>();
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
                    requestContent.Add(imageContent, filename, "image.jpg");

                    using (var x = new HttpRequestMessage(HttpMethod.Post, new Uri(BaseAddress + uri))
                    {
                        //Version = HttpVersion.Version11,
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
                Console.WriteLine($"{cookie.Name}= {cookie.Value}");
                cookieContainer.Add(cookie);
            });

            var handler = new HttpClientHandler { CookieContainer = cookieContainer };
            using (var client = new HttpClient(handler) { BaseAddress = new Uri(BaseAddress) })
            {
                client.Timeout = TimeSpan.FromSeconds(timeout);

                client.DefaultRequestHeaders.Connection.Add("keep-alive");
                client.DefaultRequestHeaders.ConnectionClose = false;

                client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");

                var response = await callAsync(client, requestUri);

                var cookieUri = new Uri(BaseAddress + requestUri);

                Cookies.Clear();
                foreach (var cookie in cookieContainer.GetCookies(cookieUri).Cast<Cookie>().Distinct())
                {
                    Console.WriteLine($"{cookie.Name}={cookie.Value}");
                    Cookies.Add(new Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain));
                }

                return await ValidateAndDeserialize<TResult>(response);
            }
        }

        private static string GetRequestUri(string method, Dictionary<string, object> parameters)
        {
            var requestUri = method.TrimEnd('/') + parameters?.DictionaryToQueryString();
            return requestUri;
        }
        public async Task<TResult> GetFormUrlEncodedAsync<TResult>(string method, int timeout = 60)
        {
            return await CreateClientAsync<TResult>(
                async (client, uri) =>
                    await client.SendAsync(
                        new HttpRequestMessage(HttpMethod.Get, method)
                    ), method, null, timeout);
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
        public async Task HttpUploadFile(string url, string file, string paramName, string contentType, NameValueCollection nvc)
        {
            //log.Debug(string.Format("Uploading {0} to {1}", file, url));
            var boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            var boundarybytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            var wr = (HttpWebRequest)WebRequest.Create(url);
            wr.ContentType = "multipart/form-data; boundary=" + boundary;
            wr.Method = "POST";
            wr.KeepAlive = true;
            //wr.Credentials = CredentialCache.DefaultCredentials;

            var rs = wr.GetRequestStream();

            const string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
            foreach (string key in nvc.Keys)
            {
                await rs.WriteAsync(boundarybytes, 0, boundarybytes.Length);
                var formitem = string.Format(formdataTemplate, key, nvc[key]);
                var formitembytes = Encoding.UTF8.GetBytes(formitem);
                await rs.WriteAsync(formitembytes, 0, formitembytes.Length);
            }
            await rs.WriteAsync(boundarybytes, 0, boundarybytes.Length);
            const string fileName = "ES_g13.8_RTF_TEF001_V8.12_V026.12_V026";
            const string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
            var header = string.Format(headerTemplate, paramName, fileName, contentType);
            var headerbytes = Encoding.UTF8.GetBytes(header);
            await rs.WriteAsync(headerbytes, 0, headerbytes.Length);

            //var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
            //var buffer = new byte[4096];
            //int bytesRead;
            //var total = 0;
            //while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
            //{
            //    await rs.WriteAsync(buffer, 0, bytesRead);
            //    total++;
            //}

            var bytes = File.ReadAllBytes(file);
            await rs.WriteAsync(bytes, 0, bytes.Length);
            //fileStream.Close();

            var trailer = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
            await rs.WriteAsync(trailer, 0, trailer.Length);
            rs.Close();

            WebResponse wresp = null;
            try
            {
                wresp = wr.GetResponse();
                //var stream2 = wresp.GetResponseStream();
                //var reader2 = new StreamReader(stream2);
                //log.Debug(string.Format("File uploaded, server response is: {0}", reader2.ReadToEnd()));
            }
            catch (Exception ex)
            {
                //log.Error("Error uploading file", ex);
                if (wresp != null)
                {
                    wresp.Close();
                    //wresp = null;
                }
            }
            finally
            {
                //wr = null;
            }

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