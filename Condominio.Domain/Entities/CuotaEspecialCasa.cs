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

  
        public decimal Monto { get; set; }

  
        public string Estado { get; set; } = "Pendiente"; // 'Pendiente', 'Pagada', 'Rechazada'

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
    }
}
