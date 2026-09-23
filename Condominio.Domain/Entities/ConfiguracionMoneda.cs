using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Domain.Entities
{
    public class ConfiguracionMoneda 
    {
        public int Id { get; set; }
        public string Moneda { get; set; } = "USD";  // 'USD' o 'EUR'
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }
}
