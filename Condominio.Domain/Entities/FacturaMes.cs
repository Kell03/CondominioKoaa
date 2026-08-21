using System;
using System.Collections.Generic;
using System.Text;

namespace Condominio.Domain.Entities
{
    public class FacturaMes:BaseEntity
    {

       public int Mes {  get; set; }
       public int Year {  get; set; }
       public bool IsActive { get; set; } = true;
       public double MontoTotal { get; set; } = 0;

        public ICollection<FacturaMesHijo> FacturaMesHijos { get; set; } = new List<FacturaMesHijo>();

    }
}
