using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Domain.Entities
{
    public class Payments : BaseEntity
    {
        // ✅ ORIGEN DEL PAGO (uno de los dos)
        public int? FacturaMesCasaId { get; set; }
        public int? CuotaEspecialCasaId { get; set; }

        // ✅ DATOS DEL PAGO
        public decimal Monto { get; set; }
        public string? Referencia { get; set; }
        public string? MetodoPago { get; set; }
        public string Estado { get; set; } = "Pendiente";
        public string? Comentario { get; set; }
        public DateTime FechaPago { get; set; } = DateTime.Now;

        // ✅ NAVEGACIÓN
        [ForeignKey(nameof(FacturaMesCasaId))]
        public FacturaMesCasa? FacturaMesCasa { get; set; }

        [ForeignKey(nameof(CuotaEspecialCasaId))]
        public CuotaEspecialCasa? CuotaEspecialCasa { get; set; }
    }
}
