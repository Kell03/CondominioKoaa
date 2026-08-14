using Condominio.Domain.Interfaces.Repositories;
using Condominio.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Condominio.Domain.Entities;

namespace Condominio.Infrastructure
{
    public static class DependencyInjection
    {

        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {

            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IGenericRepository<Users>, UserRepository>();

            // Register application services here
            return services;
        }
    }
}
