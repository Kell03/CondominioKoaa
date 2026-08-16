using System;
using System.Collections.Generic;
using System.Text;

namespace Condominio.Domain.Entities
{
    public class Users: BaseEntity
    {

        public string Name { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }

        public int? HouseId { get; set; }

        public Houses? House { get; set; }
        public bool IsActive { get; set; } = true;

    }
}
