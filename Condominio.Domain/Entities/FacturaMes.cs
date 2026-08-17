using System;
using System.Collections.Generic;
using System.Text;

namespace Condominio.Domain.Entities
{
    public class FacturaMes:BaseEntity
    {

        int Mes {  get; set; }
        int Year {  get; set; }
        bool IsActive { get; set; } = true;
        double MontoTotal { get; set; } = 0;
    }
}
