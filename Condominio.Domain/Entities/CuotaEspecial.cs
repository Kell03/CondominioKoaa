using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Domain.Entities
{
    public class CuotaEspecial : BaseEntity
    {

        public int Mes { get; set; }


        public int Year { get; set; }

        public string Motivo { get; set; }

        public string? Comentario { get; set; }

        public decimal MontoTotal { get; set; }

    
        public decimal MontoRecaudado { get; set; } = 0;

        public string Estado { get; set; } = "";

        public bool IsActive { get; set; } = true;

        public bool Enviado { get; set; } = false;


        // ✅ NAVEGACIÓN
        public ICollection<CuotaEspecialCasa> CuotaEspecialCasas { get; set; } = new List<CuotaEspecialCasa>();
    }
}
