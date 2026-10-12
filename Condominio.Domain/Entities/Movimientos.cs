using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Domain.Entities
{
    public class Movimientos
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public TipoMovimiento Tipo { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public int? IdCasa { get; set; }
        public int? IdFactura { get; set; }
        public int? IdCuotaEspecial { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navegación
        public Houses? House { get; set; }
        public FacturaMes? Factura { get; set; }
        public CuotaEspecial? CuotaEspecial { get; set; }
    }

    public enum TipoMovimiento : byte
    {
        Entrada = 1,
        Salida = 2
    }
}
