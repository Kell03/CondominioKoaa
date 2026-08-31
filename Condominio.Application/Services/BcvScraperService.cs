using HtmlAgilityPack;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Condominio.Application.Services
{
    public class BcvScraperService
    {
        private readonly IMemoryCache _cache;
        private readonly HttpClient _httpClient;
        private readonly ILogger<BcvScraperService> _logger;
        private readonly IConfiguration _configuration;

        public BcvScraperService(
            HttpClient httpClient,
            ILogger<BcvScraperService> logger,
            IMemoryCache cache,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _cache = cache;
            _configuration = configuration;
        }


        private string BcvUrl => _configuration["ExternalApis:BcvUrl"] ?? "https://www.bcv.org.ve/";

        public async Task<BcvRates> GetRatesAsync()
        {
            // ✅ INTENTAR OBTENER DEL CACHÉ
            if (_cache.TryGetValue("BCVRates", out BcvRates cachedRates))
            {
                // ✅ VERIFICAR SI DEBE ACTUALIZARSE (AM/PM)
                if (!DebeActualizar(cachedRates.Fecha))
                {
                    _logger.LogInformation($"✅ Tasas del caché: {cachedRates.Fecha:dd/MM/yyyy HH:mm}");
                    return cachedRates;
                }
            }

            // ✅ SI NO HAY CACHÉ O DEBE ACTUALIZARSE
            return await ScrapeAndCacheAsync();
        }

        private bool DebeActualizar(DateTime lastUpdate)
        {
            var now = DateTime.Now;
            var esAm = now.Hour < 12;
            var ultimaFueAm = lastUpdate.Hour < 12;

            // ✅ ACTUALIZAR SI:
            // 1. Es AM y la última actualización fue PM
            // 2. Es PM y la última actualización fue AM
            // 3. La última actualización fue hace más de 12 horas (por si acaso)
            if (esAm != ultimaFueAm)
                return true;

            // ✅ SI ES EL MISMO PERÍODO, ACTUALIZAR CADA 4 HORAS (POR SEGURIDAD)
            if ((now - lastUpdate).TotalHours > 4)
                return true;

            return false;
        }

        public async Task<BcvRates> ForceUpdateAsync()
        {
            return await ScrapeAndCacheAsync();
        }

        private async Task<BcvRates> ScrapeAndCacheAsync()
        {
            try
            {
                _logger.LogInformation($"🔄 Scraping BCV - {DateTime.Now}");
                var rates = await GetRatesFromHtmlAsync();

                if (rates.USD > 0)
                {
                    rates.Fecha = DateTime.Now;
                    _cache.Set("BCVRates", rates, TimeSpan.FromDays(1));
                    _logger.LogInformation($"✅ Tasas actualizadas: USD {rates.USD} (AM/PM: {(rates.Fecha.Hour < 12 ? "AM" : "PM")})");
                    return rates;
                }
                else
                {
                    _logger.LogWarning("⚠️ No se pudieron obtener tasas");
                    if (_cache.TryGetValue("BCVRates", out BcvRates oldRates))
                    {
                        return oldRates;
                    }
                    return new BcvRates();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error scraping BCV: {ex.Message}");
                if (_cache.TryGetValue("BCVRates", out BcvRates oldRates))
                {
                    return oldRates;
                }
                return new BcvRates();
            }
        }

        public async Task<BcvRates> GetRatesFromHtmlAsync()
        {
            try
            {
                var html = await _httpClient.GetStringAsync(BcvUrl);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var rates = new BcvRates();

                // ✅ EXTRAER CADA MONEDA POR SU ID
                rates.EUR = ExtractValueById(doc, "euro");
                rates.USD = ExtractValueById(doc, "dolar"); // Ajusta el ID
                rates.CNY = ExtractValueById(doc, "yuan"); // Ajusta el ID
                rates.RUB = ExtractValueById(doc, "rublo"); // Ajusta el ID
                rates.TRY = ExtractValueById(doc, "lira"); // Ajusta el ID

                rates.Fecha = DateTime.Now;
                return rates;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                return new BcvRates();
            }
        }


        private decimal ExtractValueById(HtmlDocument doc, string id)
        {
            var node = doc.DocumentNode
                .SelectSingleNode($"//div[@id='{id}']//strong[contains(@class, 'strong-tb')]");

            if (node == null)
                return 0;

            var valueText = node.InnerText.Trim();

            if (decimal.TryParse(
                valueText.Replace(".", "").Replace(",", "."),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal value))
            {
                // ✅ TRUNCAR A 2 DECIMALES (SIN REDONDEAR)
                return Math.Truncate(value * 100) / 100;
            }

            return 0;
        }
    }





    public class BcvRates
    {
        public decimal USD { get; set; }
        public decimal EUR { get; set; }
        public decimal CNY { get; set; }
        public decimal RUB { get; set; }
        public decimal TRY { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public bool EsValido => USD > 0;
    }
}
