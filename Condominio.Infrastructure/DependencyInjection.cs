using Condominio.Domain.Entities;
using Condominio.Domain.Interfaces.Repositories;
using Condominio.Domain.Interfaces.Services;
using Condominio.Infrastructure.Repositories;
using Condominio.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

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
            services.AddScoped<HouseRepository>();
            services.AddScoped<NotificacionRepository>();
            services.AddScoped<ConfiguracionMonedaRepository>();
            services.AddScoped<FacturaMesRepository>();//para funciones especificas
            services.AddScoped<FacturaMesHijoRepository>();
            services.AddScoped<FacturaMesCasaRepository>();
            services.AddScoped<CuotaEspecialRepository>();
            services.AddScoped<CuotaEspecialCasaRepository>();
            services.AddScoped<UserRepository>();
            services.AddScoped<MovimientosRepository>();
            services.AddScoped<CategoriaRepository>();
            services.AddScoped<ProveedorRepository>();
            services.AddScoped<GastosRepository>();
            services.AddScoped<PaymentRepository>();

            services.AddScoped<IGenericRepository<FacturaMesHijo>, FacturaMesHijoRepository>();


            services.AddScoped<IFacturaGastoRepository, FacturaGastoRepository>();

            services.AddScoped<IFacturaGastoService, FacturaGastoService>();

            services.AddScoped<GoogleDriveService>();


            // Register application services here
            return services;
        }
    }
}
