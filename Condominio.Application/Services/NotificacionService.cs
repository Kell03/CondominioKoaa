using Condominio.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Application.Services
{
    public class NotificacionService
    {

        private readonly NotificacionRepository _repo;

        public NotificacionService(NotificacionRepository repo)
        {
            _repo = repo;
        }
    }
}
