using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using TrainScrapingCommon.Models;
using TrainScrapingCommon.Models.RequestBody;

namespace TrainScrapingWorkerService
{
    class API : IDisposable
    {
        private readonly HttpClient client;

        public string BaseUrl { get; }

        public string Token { get; }

        public API(string baseUrl, string token)
        {
            BaseUrl = baseUrl;
            Token = token;
            client = new HttpClient();
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Token);
        }

        public async Task<bool> Ping()
        {
            try
            {
                return await Request("/ping", HttpMethod.Get);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        public Task<bool> PostDny(Dny dny, DateTime timestamp)
        {
            return Request("/trains/dny", HttpMethod.Post, new PostDnyBody()
            {
                Dny = dny,
                Timestamp = timestamp,
            });
        }

        public async Task<bool> Request(string requestUrl, HttpMethod method, RequestBodyBase body = null)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(method, requestUrl))
            {
                if (body != null)
                {
                    string json = JsonConvert.SerializeObject(body);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }

                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    return response.IsSuccessStatusCode;
                }
            }
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }
}
