using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SimpleWebClient
{
    public class HttpClientWrapper
    {
        public static HttpResponse Send(string serviceUrl, string acceptHeader, HttpMethod httpMethod, string postBody, string postContentType, string username, string password)
        {
            return ExecuteSend(serviceUrl, acceptHeader, httpMethod, postBody, postContentType, username, password);
        }

        public static HttpResponse Send(string serviceUrl, string acceptHeader, HttpMethod httpMethod, string postBody, string postContentType)
        {
            return ExecuteSend(serviceUrl, acceptHeader, httpMethod, postBody, postContentType, null, null);
        }

        private static HttpResponse ExecuteSend(string serviceUrl, string acceptHeader, HttpMethod httpMethod, string postBody, string postContentType, string username, string password)
        {
            using (var httpClient = new HttpClient())
            {
                if (!string.IsNullOrEmpty(username))
                {
                    AddCustomHeaders(AuthenticationHeader(username, password), httpClient);
                }

                if (!string.IsNullOrEmpty(acceptHeader))
                {
                    httpClient.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue(acceptHeader));
                }

                if (httpMethod.Method == HttpMethod.Post.Method)
                {
                    //POST
                    var response = httpClient.PostAsync(serviceUrl, new StringContent(postBody, Encoding.UTF8, postContentType)).Result;
                    return new HttpResponse
                    {
                        Content = response.Content.ReadAsStringAsync().Result,
                        Status = (int)response.StatusCode,
                        Success = response.IsSuccessStatusCode
                    };
                }

                if (httpMethod.Method == HttpMethod.Get.Method)
                {
                    //GET
                    var response = httpClient.GetAsync(serviceUrl).Result;
                    return new HttpResponse
                    {
                        Content = response.Content.ReadAsStringAsync().Result,
                        Status = (int)response.StatusCode,
                        Success = response.IsSuccessStatusCode
                    };
                }

                if (httpMethod.Method == HttpMethod.Put.Method)
                {
                    //PUT
                    var response = httpClient.PutAsync(serviceUrl, new StringContent(postBody, Encoding.UTF8, postContentType)).Result;
                    return new HttpResponse
                    {
                        Content = response.Content.ReadAsStringAsync().Result,
                        Status = (int)response.StatusCode,
                        Success = response.IsSuccessStatusCode
                    };
                }

                if (httpMethod.Method == HttpMethod.Delete.Method)
                {
                    //PUT
                    var response = httpClient.DeleteAsync(serviceUrl).Result;
                    return new HttpResponse
                    {
                        Content = response.Content.ReadAsStringAsync().Result,
                        Status = (int)response.StatusCode,
                        Success = response.IsSuccessStatusCode
                    };
                }

                throw new Exception(string.Format("[{0}] unsupported method!", httpMethod.Method));
            }
        }

        private static void AddCustomHeaders(Dictionary<string, string> customHeaders, HttpClient client)
        {
            if (customHeaders == null) return;
            foreach (var customHeader in customHeaders)
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(customHeader.Key, customHeader.Value);
            }
        }

        private static Dictionary<string, string> AuthenticationHeader(string username, string password)
        {
            var credentials = Convert.ToBase64String(Encoding.Default.GetBytes(username + ":" + password));
            var authenticationHeader = new Dictionary<string, string>
            {
                {"Authorization", string.Format("Basic {0}", credentials)}
            };
            return authenticationHeader;
        }
    }

    public class HttpResponse
    {
        public string Content { get; set; }
        public int Status { get; set; }
        public bool Success { get; set; }
    }
}
