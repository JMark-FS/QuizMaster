using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using QuizMaster.API.Authentication.Configuration;
using QuizMaster.API.Authentication.Helper;
using QuizMaster.API.Authentication.Services;
using QuizMaster.API.Authentication.Services.Auth;
using QuizMaster.API.Authentication.Services.GRPC;
using QuizMaster.API.Authentication.Services.Temp;
using QuizMaster.API.Authentication.Services.Worker;
using QuizMaster.API.Gateway.Configuration;
using QuizMaster.API.Gateway.Hubs;
using QuizMaster.API.Gateway.Services;
using QuizMaster.API.Gateway.Services.Email;
using QuizMaster.API.Gateway.Services.ReportService;
using QuizMaster.API.Gateway.Services.SystemService;
using QuizMaster.API.Gateway.SystemData.Contexts;
using QuizMaster.API.Gatewway.Controllers;
using QuizMaster.API.Quiz.Services.Workers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Inject Auto Mapper
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddControllers();
builder.Services.AddLogging();
builder.Services.AddSignalR();
builder.Services.AddCors(o =>
{
    var conf = builder.Configuration.GetSection("AppSettings:CORS_ORIGINS").Get<string[]>();
    o.AddDefaultPolicy(builder =>
    builder.WithOrigins(conf).AllowAnyHeader().AllowAnyMethod().AllowCredentials());
}
    );
builder.Services.AddDbContext<SystemDbContext>(option => option.UseSqlite("Data Source=SystemData\\System.db"));
builder.Services.AddSingleton<ReportServiceHandler>();
builder.Services.AddScoped<ReportRepository>();
builder.Services.AddScoped<SessionHub>();
builder.Services.AddSingleton<SessionHandler>();
builder.Services.AddSingleton<QuizHandler>();
builder.Services.AddScoped<SystemRepository>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "QuizMaster - API Gateway! Lez gaww baybiii!",
        Version = "v1",
    });
    // Enable token based auth in swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In=Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description="Please enter JWT Token",
        Name="Authorization",
        Type=Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        BearerFormat="JWT",
        Scheme="bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference= new OpenApiReference{
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    }) ;
});

builder.Services.AddControllers().AddNewtonsoftJson();

// Configuring strongly typed settings object
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.Configure<ApplicationSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.Configure<GrpcServerConfiguration>(builder.Configuration.GetSection("GrpcServerConfiguration"));

// register the services
builder.Services.AddScoped<IRepository, Repository>();
builder.Services.AddScoped<RabbitMqUserWorker>();
builder.Services.AddScoped<IAuthenticationServices, AuthenticationServices>();
builder.Services.AddSingleton<RabbitMqRepository>();
builder.Services.AddScoped<AccountGatewayController>();
builder.Services.AddSingleton<IDictionary<string, int>>(o => new Dictionary<string, int>());
builder.Services.AddScoped<EmailService>();

// Use razor pages
builder.Services.AddRazorPages();


// configure cookie authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(option =>
    {
        option.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async (context) =>
            {
                // validate the cookie
                await CookieHelper.ValidateCookie(context, builder.Configuration["AppSettings:JWTSecret"]);
            }
        };
        option.ExpireTimeSpan = TimeSpan.FromHours(Convert.ToInt16(builder.Configuration["AppSettings:IntExpireHour"]));
        option.SlidingExpiration = true; // renew cookie when it's about to expire,
        option.Cookie.SameSite = SameSiteMode.None; // frontend is using different port
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c =>
    {
        c.PreSerializeFilters.Add((swagger, httpReq) =>
        {
            swagger.Servers = new List<OpenApiServer> { new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}" } };
        });
    });
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway - Documentation");
    });
}
app.UseSwagger(c =>
{
    c.PreSerializeFilters.Add((swagger, httpReq) =>
    {
        swagger.Servers = new List<OpenApiServer> { new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}" } };
    });
});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway - Documentation");
});


//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();


app.UseCors(options => options.SetIsOriginAllowed(x => _ = true).AllowAnyMethod().AllowCredentials().AllowAnyHeader());

app.MapControllers();
app.MapHub<SessionHub>("/gateway/hub/session");

// Initialize the Database
using(var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var systemDbContext = services.GetRequiredService<SystemDbContext>();
    systemDbContext.Database.EnsureCreated();
}

app.Run();
