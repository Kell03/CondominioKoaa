using System;
using System.Collections.Generic;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Condominio.Domain.Entities
{
    public class Houses: BaseEntity
    {

        public string Number { get; set; }
        public string Street { get; set; }
        public string? AddressComplement { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
