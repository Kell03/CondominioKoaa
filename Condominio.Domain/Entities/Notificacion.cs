using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Domain.Entities
{
    public class Notificacion : BaseEntity
    {
        public int UserId { get; set; }
        public string Titulo { get; set; }
        public string Mensaje { get; set; }
        public string Tipo { get; set; }
        public int? ReferenciaId { get; set; }
        public bool IsRead { get; set; } = false;  // ✅ EF lo mapea a TINYINT
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaLeida { get; set; }

        // ✅ NAVEGACIÓN (RECOMENDADA)
        public Users User { get; set; }
    }
}
