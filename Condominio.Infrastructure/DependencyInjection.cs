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
            services.AddScoped<IGenericRepository<Houses>, HouseRepository>();
            services.AddScoped<IGenericRepository<FacturaMes>, FacturaMesRepository>();
            services.AddScoped<IGenericRepository<FacturaMesCasa>, FacturaMesCasaRepository>();
            services.AddScoped<FacturaMesRepository>();//para funciones especificas
            services.AddScoped<FacturaMesCasaRepository>();
            services.AddScoped<CuotaEspecialRepository>();
            services.AddScoped<CuotaEspecialCasaRepository>();
            services.AddScoped<UserRepository>();

            services.AddScoped<IGenericRepository<FacturaMesHijo>, FacturaMesHijoRepository>();

          
            // Register application services here
            return services;
        }
    }
}
