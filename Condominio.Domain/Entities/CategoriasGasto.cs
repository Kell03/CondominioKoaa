using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Domain.Entities
{
    public class CategoriasGasto : BaseEntity
    {

        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; } = true;

        // Navegación
       // public ICollection<Proveedor> Proveedores { get; set; } = new List<Proveedor>();
    }
}
