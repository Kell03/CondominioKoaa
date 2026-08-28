using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Condominio.Domain.Entities;

namespace Condominio.Application.Services
{
    public class MonedaApiService
    {

        private readonly HttpClient _httpClient;

        // ✅ URL QUEMADA DIRECTAMENTE
        // private readonly string _baseUrl = "https://ve.dolarapi.com/v1/dolares/oficial";
        private readonly string _baseUrl = "https://ve.dolarapi.com/v1/euros/oficial";

        public MonedaApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ApiMoneda> GetMonedaAsync()
        {

            try
            {
                string url = $"{_baseUrl}";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ApiMoneda>();

            }
            catch (Exception ex)
            {
               throw ;
            }
        }

    }
}
