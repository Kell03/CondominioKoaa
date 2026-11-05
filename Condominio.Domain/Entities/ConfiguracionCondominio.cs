using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Domain.Entities
{
    public class ConfiguracionCondominio
    {

        [Key]
        public int Id { get; set; }

        // ✅ Solo el nombre es obligatorio
        [Required(ErrorMessage = "El nombre de la urbanización es obligatorio")]
        public string NombreUrbanizacion { get; set; } = string.Empty;

        // El resto son nullable (string? en C#)
        public string? BancoNombre { get; set; }
        public string? BancoCuenta { get; set; }
        public string? BancoRif { get; set; }

        public string? PagoMovilTelefono { get; set; }
        public string? PagoMovilCedula { get; set; }

        // Base64 de la imagen
        public string? LogoBase64 { get; set; }
    }
}
