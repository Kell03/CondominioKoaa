using System;
using System.Collections.Generic;
using System.Text;

namespace Condominio.Domain.Entities
{
    public class FacturaMesHijo : BaseEntity
    {
        public int FacturaMesId { get; set; }
        public FacturaMes facturaMes { get; set; } 
        public string Motivo { get; set; } = string.Empty;
        public double Monto { get; set; } = 0;
    }
}
