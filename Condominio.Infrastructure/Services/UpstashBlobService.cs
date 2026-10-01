using Amazon.S3;
using Amazon.S3.Model;
using Condominio.Domain.Entities;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Infrastructure.Services
{
    public class UpstashBlobService
    {

        private readonly HttpClient _http;
        private readonly string _token;

        public UpstashBlobService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _token = config["Upstash:BlobToken"]!;
        }

        // 1. Pide credenciales temporales a Upstash
        private async Task<UpstashCredentials> ObtenerCredencialesAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://blob.upstash.io/v1/credentials");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<UpstashCredentials>()
                   ?? throw new Exception("No se pudieron obtener credenciales de Upstash");
        }

        // 2. Sube el archivo usando las credenciales temporales
        public async Task<string> SubirAsync(Stream fileStream, string nombreArchivo, string contentType)
        {
            var creds = await ObtenerCredencialesAsync();

            var s3Config = new AmazonS3Config
            {
                ServiceURL = creds.Endpoint,
                ForcePathStyle = true,
                AuthenticationRegion = "auto"
            };

            var s3Client = new AmazonS3Client(
                creds.AccessKeyId,
                creds.SecretAccessKey,
                creds.SessionToken,
                s3Config
            );

            var key = $"facturas/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid()}-{nombreArchivo}";

            var putRequest = new PutObjectRequest
            {
                BucketName = creds.Bucket,
                Key = key,
                InputStream = fileStream,
                ContentType = contentType,
                DisablePayloadSigning = true  

            };

            await s3Client.PutObjectAsync(putRequest);

            return key; // esto guardas en MySQL
        }


        public async Task<string> ObtenerUrlLecturaAsync(string key, int minutosValidez = 15)
        {
            var creds = await ObtenerCredencialesAsync();

            var s3Config = new AmazonS3Config
            {
                ServiceURL = creds.Endpoint,
                ForcePathStyle = true,
                AuthenticationRegion = "auto"
            };

            var s3Client = new AmazonS3Client(
                creds.AccessKeyId,
                creds.SecretAccessKey,
                creds.SessionToken,
                s3Config
            );

            var request = new GetPreSignedUrlRequest
            {
                BucketName = creds.Bucket,
                Key = key,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddMinutes(minutosValidez)
            };

            return s3Client.GetPreSignedURL(request);
        }

    }
}
