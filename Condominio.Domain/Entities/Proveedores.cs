using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Domain.Entities
{
    public class Proveedor: BaseEntity
    {
        public string Nombre { get; set; } = string.Empty;
        public string? NIT { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Direccion { get; set; }
        public string? Contacto { get; set; }
        public string? Banco { get; set; }
        public string? CuentaBancaria { get; set; }
        public string? Notas { get; set; }
        public bool Activo { get; set; } = true;
    }
}
