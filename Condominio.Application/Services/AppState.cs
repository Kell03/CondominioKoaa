using Condominio.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Application.Services
{
    public class AppState
    {
        public Users CurrentUser { get; set; }
    }
}
