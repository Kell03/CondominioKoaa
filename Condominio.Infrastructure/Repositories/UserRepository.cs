using System;
using System.Collections.Generic;
using System.Text;
using Condominio.Domain.DB;
using Condominio.Domain.Entities;
namespace Condominio.Infrastructure.Repositories
{
    public class UserRepository: GenericRepository<Users>
    {
        public UserRepository(AppDbContext context) : base(context) { }

       


    }
}
