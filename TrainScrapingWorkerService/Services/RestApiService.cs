﻿using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using TrainScrapingCommon.Models.Dnys;
using TrainScrapingCommon.Models.RequestBody;

namespace TrainScrapingWorkerService.Services
{
    class RestApiService : IApiService
    {
        private readonly HttpClient client;

        public RestApiService(string baseUrl, string token)
        {
            client = new HttpClient();
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
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

        public Task<bool> PostDny(DnyPost dny, DateTime timestamp)
        {
            return Request("/trains/dny", HttpMethod.Post, new PostDnyBody()
            {
                Dny = dny,
                Timestamp = timestamp,
            });
        }

        public async Task<bool> Request(string requestUrl, HttpMethod method, RequestBodyBase? body = null)
        {
            using HttpRequestMessage request = new HttpRequestMessage(method, requestUrl);
            if (body != null)
            {
                string json = JsonConvert.SerializeObject(body);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            using HttpResponseMessage response = await client.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }
}
