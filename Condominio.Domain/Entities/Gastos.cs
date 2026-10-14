using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Domain.Entities
{
    public class Gasto
    {
        public int Id { get; set; }
        public int? FacturaMesHijoId { get; set; }
        public int? CuotaEspecialId { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaPago { get; set; }
        public string? Comprobante { get; set; }
        public string? Notas { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navegación
        public FacturaMesHijo? FacturaMesHijo { get; set; }
        public CuotaEspecial? CuotaEspecial { get; set; }
    }
}
