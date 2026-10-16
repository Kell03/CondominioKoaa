using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Condominio.Domain.Entities
{
    public class FacturaMesHijo : BaseEntity
    {
        public int FacturaMesId { get; set; }
        public int? IdCategoria { get; set; }
        public int? IdProveedor { get; set; }
        public FacturaMes facturaMes { get; set; } 
        public string Motivo { get; set; } = string.Empty;
        public double Monto { get; set; } = 0;
        public bool Pagado { get; set; } = false;
        public CategoriasGasto? CategoriaGasto { get; set; }   // ← NUEVO
        public Proveedor? Proveedor { get; set; }   // ← NUEVO

    }
}
