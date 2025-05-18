using ShareVault.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using System.Reflection;
using ShareVault.API.Services;
using ShareVault.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// CORS yapılandırması
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("http://localhost:3000") // React uygulamasının adresi
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

// Swagger yapılandırması
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "ShareVault API", 
        Version = "v1",
        Description = "ShareVault - Güvenli Dosya Paylaşım API'si",
        Contact = new OpenApiContact
        {
            Name = "ShareVault Support",
            Email = "support@sharevault.com"
        }
    });

    // XML dosyasından API açıklamalarını oku
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);

    // JWT Bearer token yapılandırması
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var key = builder.Configuration["Jwt:Key"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        };
    });

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICacheService, CacheService>();

builder.Services.AddControllers();

var app = builder.Build();

// Swagger middleware'ini ekle
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ShareVault API v1");
        c.RoutePrefix = string.Empty; // Swagger'ı root URL'de göster
    });
}

app.UseRouting();

// CORS middleware'ini ekle
app.UseCors();

// Global exception middleware'ini ekle
app.UseMiddleware<GlobalExceptionMiddleware>();

// Request logging middleware'ini ekle
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseHttpsRedirection();

// Statik dosyaları sunmak için
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
