using BMapr.GDAL.WebApi;
using BMapr.GDAL.WebApi.Authentication.Base;
using BMapr.GDAL.WebApi.Models.Job;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using OSGeo.GDAL;
using OSGeo.MapServer;
using OSGeo.OGR;
using Quartz;
using Quartz.AspNetCore;
using Serilog;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using Microsoft.OpenApi;

/**********************************************************************************
 * Builder
 **********************************************************************************/

//GdalConfiguration.ConfigureGdal();
//GdalConfiguration.ConfigureOgr();

var path = GdalConfiguration.IniPath();
Gdal.AllRegister();
Ogr.RegisterAll();
mapscript.SetEnvironmentVariable(path);

var builder = WebApplication.CreateBuilder(args);

if (args.Any(x => x.ToLower().StartsWith("port=")))
{
    var portString = args.First(x => x.ToLower().StartsWith("port=")).Replace("port=","");
    int port;

    if (int.TryParse(portString, out port))
    {
        Console.WriteLine($"use port {port}");
        builder.WebHost.ConfigureKestrel((context, serverOptions) =>
        {
            serverOptions.Listen(IPAddress.Any, port);
        });
    }
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "BMapr",
        Description = ".Net 9 Web API, provide OGC service interface and other helpers based on GDAL/OGR MapServer, for example add WMTS and WFS-T functionality",
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddSingleton<IWebHostEnvironment>(builder.Environment);

builder.Services.AddMemoryCache();
builder.Services.AddAuthentication("BasicAuthentication").AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);
builder.Services.AddAuthorization(options => { options.AddPolicy("BasicAuthentication", new AuthorizationPolicyBuilder("BasicAuthentication").RequireAuthenticatedUser().Build());});

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

var service = builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();

    var jobKey = new JobKey("JobEngine");
    var currentProcess = Process.GetCurrentProcess();

    q.AddJob<JobEngine>(opts => opts.WithIdentity(jobKey).UsingJobData("processId", currentProcess.Id));

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("JobEngine-Trigger")
        .StartNow());
});

builder.Services.AddQuartzServer(options =>
{
    options.WaitForJobsToComplete = true;
});


/**********************************************************************************
 * Application
 **********************************************************************************/

var configValue = string.IsNullOrEmpty(builder.Configuration.GetValue<string>("Settings:WebApplication")) ? "" : $"/{builder.Configuration.GetValue<string>("Settings:WebApplication")}";
var app = builder.Build();

ServiceLocator.ServiceProvider = app.Services;

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.InjectStylesheet($"{configValue}/swagger-ui/custom.css");
    options.InjectJavascript($"{configValue}/swagger-ui/custom.js");
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}");

app.MapControllers();

app.UseCors(x => x
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

app.Lifetime.ApplicationStarted.Register(OnStarted);
app.Lifetime.ApplicationStopped.Register(OnStopped);

app.Run();

void OnStarted()
{
    app.Logger.LogInformation("app starts");
}

void OnStopped()
{
    app.Logger.LogInformation("end starts");
}

public class ServiceLocator
{
    public static IServiceProvider ServiceProvider { get; set; }
}