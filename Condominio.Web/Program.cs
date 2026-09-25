using Condominio.Application;
using Condominio.Application.Services;
using Condominio.Domain.DB;
using Condominio.Domain.Interfaces;
using Condominio.Infrastructure;
using Condominio.Infrastructure.Hubs;
using Condominio.Infrastructure.Services;
using Condominio.Web.Components;
using Condominio.Web.Endpoints;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.EntityFrameworkCore;
using Radzen;  

var builder = WebApplication.CreateBuilder(args);

// ✅ 1. CONFIGURAR KESTREL PARA ESCUCHAR EN TODAS LAS INTERFACES
//builder.WebHost.ConfigureKestrel(options =>
//{
//    options.ListenAnyIP(5000);
//});
//
//// ✅ 2. CONFIGURAR COOKIES PARA ACEPTAR CONEXIONES EXTERNAS
//builder.Services.ConfigureApplicationCookie(options =>
//{
//    options.Cookie.SameSite = SameSiteMode.Lax;
//    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // ✅ CLAVE: NO FORZAR HTTPS
//});
//builder.WebHost.UseUrls("http://0.0.0.0:5000");


StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

// Add services to the container.
builder.Services.AddRazorComponents();

// ✅ 1. Servicios de Blazor Server (CORRECTO)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();  // ← Esto es OBLIGATORIO

// ✅ 2. Radzen
builder.Services.AddRadzenComponents();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");


builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseMySql(connectionString,
        ServerVersion.AutoDetect(connectionString))
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
    .EnableDetailedErrors(builder.Environment.IsDevelopment()));

builder.Services.AddSignalR();

builder.Services.AddScoped<CustomAuthStateProvider>();  // ← ESTO ES CLAVE
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AppState>();
builder.Services.AddHttpClient<MonedaApiService>();
builder.Services.AddScoped<MonedaApiService>();
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.Configuration["BaseUrl"]
        ?? "https://localhost:7292")  // 👈 cambia por tu puerto
});
// ✅ SERVICIO
builder.Services.AddScoped<BcvScraperService>();
builder.Services.AddScoped<INotificacionRealTimeService, NotificacionRealTimeService>();

// ✅ HTTPCLIENT
builder.Services.AddHttpClient<BcvScraperService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
});

// ✅ MEMORY CACHE
builder.Services.AddMemoryCache();

builder.Services.AddApplication();
builder.Services.AddInfrastructure();



builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;   // ⭐ CAMBIO
    });

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// 1️⃣ PRIMERO: Manejo de errores
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/error/{0}");

// 2️⃣ DESPUÉS: Archivos estáticos y HTTPS
app.UseHttpsRedirection();
app.UseStaticFiles();

// 3️⃣ ROUTING
app.UseRouting();

// 4️⃣ AUTENTICACIÓN Y AUTORIZACIÓN (DESPUÉS de UseRouting)
app.UseAuthentication();
app.UseAuthorization();

// 5️⃣ ANTIFORGERY
app.UseAntiforgery();
app.MapAuthEndpoints();           // ← 2) agregar antes de app.Run()


// 6️⃣ ENDPOINTS
app.MapHub<NotificationHub>("/notificacionHub");   // ⭐  // ✅ RequireAuthorization
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


app.Run();