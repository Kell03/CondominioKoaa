using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Domain.Interfaces
{
    public interface INotificacionRealTimeService
    {

        Task EnviarNotificacionAsync(int userId);
        Task EnviarNotificacionMultipleAsync(List<int> userIds);

    }
}
