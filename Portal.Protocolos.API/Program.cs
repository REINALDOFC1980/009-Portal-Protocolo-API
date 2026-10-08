using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Portal.Protocolos.API.Middleware;
using Portal.Protocolos.API.Models.Seguranca.GovBr;
using Portal.Protocolos.API.Repositories;
using Portal.Protocolos.API.Service;
using Portal.Protocolos.API.Service.Atendimento;
using Portal.Protocolos.API.Service.Cadastros;
using Portal.Protocolos.API.Service.Seguranca;
using Portal.Protocolos.API.Services.Atendimento;
using Portal.Protocolos.API.Services.Integracao;
using Portal.Protocolos.Infrastructure.Database.Connection;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();


builder.Services.Configure<GovBrSettings>(builder.Configuration.GetSection("GovBr"));

builder.Services.AddScoped<UsuarioAutenticacaoRepository>();
builder.Services.AddScoped<UsuarioPerfilRepository>();
builder.Services.AddScoped<UsuarioRepository>();
builder.Services.AddScoped<ServicoRepository>();
builder.Services.AddScoped<ProcessoRepository>();
builder.Services.AddScoped<ProcessoCondutorRepository>();
builder.Services.AddScoped<ProcessoAnexoRepository>();
builder.Services.AddScoped<ProcessoMovimentacaoRepository>();
builder.Services.AddScoped<AutoInfracaoRepository>();
builder.Services.AddScoped<SolicitanteAtendimentoRepository>();
builder.Services.AddHttpClient<GovBrService>();
builder.Services.AddScoped<ProcessoPresencialDetalheRepository>();
builder.Services.AddScoped<RegraAnexoRepository>();
builder.Services.AddScoped<AgendamentosPendentesRepository>();
builder.Services.AddScoped<ProcessoAtendimentoRepository>();
builder.Services.AddScoped<CondutorPresencialRepository>();
builder.Services.AddScoped<CopiaProcessoRepository>();
builder.Services.AddScoped<UsuarioInternoRepository>();

builder.Services.AddScoped<SolicitanteAtendimentoService>();
builder.Services.AddScoped<ServicoDisponivelService>();
builder.Services.AddScoped<ResponsavelAitService>();
builder.Services.AddScoped<AtendimentoAitService>();
builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<ProcessoService>();
builder.Services.AddScoped<CondutorService>();
builder.Services.AddScoped<DefesaService>();
builder.Services.AddScoped<JariService>();
builder.Services.AddScoped<CetranService>();
builder.Services.AddScoped<RessarcimentoService>();
builder.Services.AddScoped<AgendamentoConsultaService>();
builder.Services.AddScoped<DefesaPresencialService>();
builder.Services.AddScoped<JariPresencialService>();
builder.Services.AddScoped<CondutorPresencialService>();
builder.Services.AddScoped<ProcessoAnexoService>();
builder.Services.AddScoped<ProcessoMovimentacaoService>();
builder.Services.AddScoped<AutoInfracaoService>();
builder.Services.AddScoped<ProcessoPresencialDetalheService>();
builder.Services.AddScoped<RegraAnexoService>();
builder.Services.AddScoped<AgendamentosPendentesService>();
builder.Services.AddScoped<AgendamentoAtendimentoService>();
builder.Services.AddScoped<CopiaProcessoService>();
builder.Services.AddScoped<UsuarioInternoService>();


builder.Services.AddHttpClient<SitranApiClient>((serviceProvider, client) =>
{
    var configuration =
        serviceProvider.GetRequiredService<IConfiguration>();

    var baseUrl =
        configuration["Sitran:BaseUrl"]
        ?? throw new InvalidOperationException(
            "Sitran:BaseUrl não configurado.");

    var apiKey =
        configuration["Sitran:ApiKey"]
        ?? throw new InvalidOperationException(
            "Sitran:ApiKey não configurado.");

    client.BaseAddress =
        new Uri(baseUrl.TrimEnd('/') + "/");

    client.DefaultRequestHeaders.Add(
        "X-API-Key",
        apiKey);

    client.Timeout =
        TimeSpan.FromSeconds(30);
});


// Controllers
builder.Services.AddControllers();



// Database
builder.Services.AddSingleton<SqlConnectionFactory>();
builder.Services.AddScoped<DbSession>();




builder.Services.AddCors(options =>
{
    options.AddPolicy("Front", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:8080",
                "http://localhost:8082",
                "https://localhost",

                // Portal Institucional
                "https://portal.178.128.150.252.nip.io",

                // Portal Presencial - IP
                "http://10.83.126.5:8088",
                "http://10.83.126.5:8089",

                // Transonline
                "http://transonline3.salvador.ba.gov.br",
                "https://transonline3.salvador.ba.gov.br"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AtendimentoInterno", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("Canal", "INTERNO");
        policy.RequireRole(
            "ATENDENTE",
            "ANALISTA",
            "SUPERVISOR",
            "ADMINISTRADOR");
    });
});


// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var securityScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Informe o token JWT.",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new Microsoft.OpenApi.Models.OpenApiReference
        {
            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    options.AddSecurityDefinition("Bearer", securityScheme);

    options.AddSecurityRequirement(
        new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                securityScheme,
                Array.Empty<string>()
            }
        });
});

var app = builder.Build();

app.UseCors("Front");

app.UseMiddleware<ExceptionMiddleware>();

// Swagger também disponível em produção
app.UseSwagger();

app.UseSwaggerUI(options =>
{
    // IMPORTANTE:
    // caminho relativo para funcionar dentro de /portalprotocoloAPI
    options.SwaggerEndpoint(
        "v1/swagger.json",
        "Portal Protocolos API v1"
    );
});

// Pode manter se o IIS está configurado corretamente para HTTPS
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
