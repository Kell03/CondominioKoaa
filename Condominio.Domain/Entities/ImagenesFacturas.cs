using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Domain.Entities
{
    public class ImagenesFacturas
    {

        public int Id { get; set; }
        public int Mes { get; set; }           // 1-12
        public int Anio { get; set; }          // 2026
        public string? Comentario { get; set; }
        public DateTime FechaSubida { get; set; } = DateTime.UtcNow;

        // Datos de Upstash
        public string RutaAlmacenamiento { get; set; } = ""; // la key
        public string NombreOriginal { get; set; } = "";
        public string TipoMime { get; set; } = "";
        public long TamanoBytes { get; set; }

    }
}
