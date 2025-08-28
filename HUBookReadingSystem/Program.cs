using System.Net;
using HUBookReadingSystem.Data;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");


// --- DB (PostgreSQL / Railway) ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));

// --- Controllers ---
builder.Services.AddControllers();

// --- Swagger (sadece Dev) ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hazal & Umut Book Reading API",
        Version = "v1",
        Description = "Kitap okuma takip sistemi"
    });
});

// --- CORS (Cookie ile, Netlify prod + local) ---
const string CorsPolicy = "AppCors";
var staticOrigins = new[]
{
    "http://127.0.0.1:5501",
    "http://localhost:5501",
    "http://127.0.0.1:5500",
    "http://localhost:5500",
    "https://hubooksystem.netlify.app" // ana prod domainin
};
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        policy
            // preview deploy’lar için *.netlify.app hostlarýný kabul et
            .SetIsOriginAllowed(origin =>
            {
                try
                {
                    var uri = new Uri(origin);
                    return staticOrigins.Contains(origin)
                        || uri.Host.EndsWith("netlify.app", StringComparison.OrdinalIgnoreCase);
                }
                catch { return false; }
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// --- Forwarded headers (Railway/Proxy) ---
builder.Services.Configure<ForwardedHeadersOptions>(opts =>
{
    opts.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor;
    // Eðer spesifik proxy IP’ni bilmiyorsan aþaðýyý EKLEME.
    // opts.KnownProxies.Add(IPAddress.Parse("x.x.x.x"));
    opts.RequireHeaderSymmetry = false;
    opts.ForwardLimit = null; // zincir halinde proxy'lerde sorun olmasýn
});

var app = builder.Build();

// Proxy header’larýný en baþta iþle
app.UseForwardedHeaders();

// Prod hardening: Swagger kapat + global exception + HSTS
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Book Reading API v1"));
}
else
{
    app.UseExceptionHandler("/error"); // basit 500 JSON döndüren endpoint’in olabilir (opsiyonel)
    app.UseHsts();
}

// HTTPS yönlendirme + CORS
app.UseHttpsRedirection();
app.UseCors(CorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => "OK");

app.Run();