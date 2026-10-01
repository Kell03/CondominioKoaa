using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Condominio.Domain.Entities
{
    public class UpstashCredentials
    {
        [JsonPropertyName("endpoint")]
        public string Endpoint { get; set; } = "";

        [JsonPropertyName("accessKeyId")]
        public string AccessKeyId { get; set; } = "";

        [JsonPropertyName("secretAccessKey")]
        public string SecretAccessKey { get; set; } = "";

        [JsonPropertyName("sessionToken")]
        public string SessionToken { get; set; } = "";

        [JsonPropertyName("bucket")]
        public string Bucket { get; set; } = "";

        [JsonPropertyName("expiresAt")]
        public long ExpiresAt { get; set; }
    }
}
