using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WishListAPI.Data;
using WishListAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// ✅ FORÇA leitura de variáveis de ambiente
builder.Configuration.AddEnvironmentVariables();

// ✅ Lê connection string de variável de ambiente OU appsettings
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// ✅ LOG para debug
Console.WriteLine($"[DEBUG] Connection String: {(string.IsNullOrEmpty(connectionString) ? "VAZIA!" : "OK")}");

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("❌ Connection string não encontrada!");
}

// Configuração do DbContext com MySQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Configuração de Controllers com JSON options para evitar ciclos
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();

// Configuração do Swagger com autenticação JWT
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "WishList API", Version = "v1" });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando Bearer scheme. Exemplo: \"Authorization: Bearer {token}\"",
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

// ✅ Lê JWT Key de variável de ambiente
var jwtKey = Environment.GetEnvironmentVariable("Jwt__Key") 
    ?? builder.Configuration["Jwt:Key"] 
    ?? "ChaveSecretaSuperSeguraParaOWishList2024!@#$%";
    
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = "WishListAPI",
        ValidAudience = "WishListUsers",
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Registrar serviços
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<WishService>();
builder.Services.AddScoped<ProgressService>();
builder.Services.AddScoped<AuthService>();

// ✅ CORS - CONFIGURAÇÃO PARA PRODUÇÃO
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>();
        var hasOrigins = origins != null && origins.Length > 0 && origins.All(o => !string.IsNullOrWhiteSpace(o));

        if (hasOrigins)
        {
            policy.WithOrigins(origins!)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
        else
        {
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        }
    });
});

// ✅ CONFIGURAR PORTA PARA O RENDER (usa variável PORT ou padrão 10000)
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
Console.WriteLine($"[DEBUG] Porta configurada: {port}");

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(int.Parse(port));
});

var app = builder.Build();

// Garante que as tabelas sejam criadas/atualizadas em produção.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

// ✅ Swagger em produção também (opcional, facilita testes)
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
