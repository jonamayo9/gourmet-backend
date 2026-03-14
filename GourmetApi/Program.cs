using GourmetApi.Data;
using GourmetApi.Models;
using GourmetApi.Services;
using MercadoPago.Config;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Gourmet API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresá: Bearer {tu token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

//(Postgres)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Front", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowAnyOrigin();
    });
});

var jwt = builder.Configuration.GetSection("Jwt");
var key = jwt["Key"] ?? throw new Exception("Jwt:Key missing");

builder.Services
  .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
      var jwt = builder.Configuration.GetSection("Jwt");
      options.TokenValidationParameters = new TokenValidationParameters
      {
          ValidateIssuer = true,
          ValidateAudience = true,
          ValidateLifetime = true,
          ValidateIssuerSigningKey = true,
          ValidIssuer = jwt["Issuer"],
          ValidAudience = jwt["Audience"],
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
          RoleClaimType = ClaimTypes.Role,
          NameClaimType = ClaimTypes.NameIdentifier
      };
  });

builder.Services.AddAuthorization();

    builder.Services.AddSingleton<CloudinaryService>();
builder.Services.Configure<MercadoPagoOptions>(
    builder.Configuration.GetSection("MercadoPago"));

builder.Services.AddScoped<MercadoPagoService>();
builder.Services.AddScoped<OrderPricingService>();

var app = builder.Build();

var mpOptions = app.Services.GetRequiredService<IOptions<MercadoPagoOptions>>().Value;
MercadoPagoConfig.AccessToken = mpOptions.AccessToken;

// (crea datos si no existen)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    await DbSeeder.SeedAsync(db, cfg);
}



app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// ✅ Static files: wwwroot
app.UseStaticFiles();

// ✅ Static files: /uploads (wwwroot/uploads)
var uploadsPath = Path.Combine(app.Environment.WebRootPath!, "uploads");
if (!Directory.Exists(uploadsPath))
    Directory.CreateDirectory(uploadsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseCors("Front");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

var mpSection = app.Configuration.GetSection("MercadoPago");
var mpToken = mpSection["AccessToken"];

Console.WriteLine("MP TOKEN CONFIG:");
Console.WriteLine(string.IsNullOrWhiteSpace(mpToken) ? "VACIO" : mpToken);


app.Run();