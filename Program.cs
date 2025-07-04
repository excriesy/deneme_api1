using ShareVault.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using System.Reflection;
using ShareVault.API.Services;
using ShareVault.API.Middleware;
using ShareVault.API.Interfaces;
using ShareVault.API.Repositories;
using ShareVault.API.Mappings;
using ShareVault.API.Models;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// CORS yapılandırması
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("http://localhost:3000", "http://localhost:5173") // React ve Vite uygulamalarının adresleri
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

var key = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured in appsettings.json");

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
builder.Services.AddSingleton<ILogService, LogService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IBruteForceProtectionService, BruteForceProtectionService>();
builder.Services.AddScoped<ShareVault.API.Interfaces.IFileService, ShareVault.API.Services.FileService>();
builder.Services.AddScoped<IFolderService, FolderService>();
builder.Services.AddHttpContextAccessor();

// Repository registrations
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();

// AutoMapper registration
builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IVersioningService, VersioningService>();

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

// Global hata yönetimi middleware'ini ekle
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseHttpsRedirection();

// Statik dosyaları sunmak için
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
