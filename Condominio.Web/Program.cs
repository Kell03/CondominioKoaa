using Condominio.Application;
using Condominio.Domain.DB;
using Condominio.Infrastructure;
using Condominio.Web.Components;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.EntityFrameworkCore;
using Radzen;  

var builder = WebApplication.CreateBuilder(args);

StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

// Add services to the container.
builder.Services.AddRazorComponents();

// ✅ 1. Servicios de Blazor Server (CORRECTO)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();  // ← Esto es OBLIGATORIO

// ✅ 2. Radzen
builder.Services.AddRadzenComponents();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);


builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();  // ← ¡ESTA LÍNEA ES CLAVE!


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>();

app.Run();
