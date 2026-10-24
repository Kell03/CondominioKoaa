using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Domain.Entities
{
    public class CuotaEspecialCasa : BaseEntity
    {
  
        public int CuotaEspecialId { get; set; }

  
        public int HouseId { get; set; }

  
        public decimal MontoTotal { get; set; }

        public decimal MontoPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string Estado { get; set; } = EstadoCuotaCasa.Pendiente; // 'Pendiente', 'Pagada', 'Rechazada'

        public string? Referencia { get; set; }

        public string? MetodoPago { get; set; }

        public DateTime? FechaPago { get; set; }

        public string? Comentario { get; set; }

   

        public decimal? MontoBs { get; set; }

        // ✅ NAVEGACIÓN
        public CuotaEspecial CuotaEspecial { get; set; } 

        public Houses House { get; set; }


        [NotMapped]
        public Users? User { get; set; }

        public ICollection<Payments>? Payments { get; set; }

        public string PeriodoCompleto => CuotaEspecial != null
? $"{CuotaEspecial.NombreMes} {CuotaEspecial.Year}"
: "-";


    }

    public static class EstadoCuotaCasa
    {
        public const string Pendiente = "Pendiente";
        public const string PagoParcial = "Pago Parcial"; // ✅ Con el espacio exacto
        public const string Confirmada = "Confirmada";
        public const string Rechazada = "Rechazada";
    }
}
