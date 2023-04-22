using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Schema = Signum.Engine.Maps.Schema;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using Signum.API.Filters;
using Signum.API;
using Signum.Rest;
using Signum.Dynamic;
using Signum.Authorization;
using Signum.Alerts;
using Signum.ConcurrentUser;
using Signum.Processes;
using Signum.Scheduler;
using Signum.Mailing;
using Signum.Cache;
using Signum.Files;
using Signum.UserQueries;
using Signum.Dashboard;
using Signum.Word;
using Signum.Excel;
using Signum.Chart;
using Signum.Map;
using Signum.Toolbar;
using Signum.Translation;
using Signum.Translation.Translators;
using Signum.Profiler;
using Signum.DiffLog;
using Signum.MachineLearning;
using Signum.Workflow;
using Signum.Omnibox;
using Southwind.Public;
using Microsoft.AspNetCore.Mvc;
using Signum.Chart.UserChart;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;

namespace Southwind;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddResponseCompression();

        builder.Services
            .AddMvc(options => options.AddSignumGlobalFilters())
            .AddApplicationPart(typeof(SignumServer).Assembly)
            .AddApplicationPart(typeof(AuthServer).Assembly)
            .AddJsonOptions(options => options.AddSignumJsonConverters());
        builder.Services.AddSignalR();
        builder.Services.AddSignumValidation();
        builder.Services.Configure<IISServerOptions>(a => a.AllowSynchronousIO = true); //JSon.Net requires it

        SwaggerConfig.ConfigureSwaggerService(builder);

        var app = builder.Build();

        app.UseDeveloperExceptionPage();

        app.UseStaticFiles();

        //HeavyProfiler.Enabled = true;
        using (HeavyProfiler.Log("Startup"))
        using (var log = HeavyProfiler.Log("Initial"))
        {
            DynamicLogic.CodeGenDirectory = app.Environment.ContentRootPath + "/CodeGen";

            Starter.Start(
                app.Configuration.GetConnectionString("ConnectionString")!,
                app.Configuration.GetValue<bool>("IsPostgres"),
                app.Configuration.GetConnectionString("AzureStorageConnectionString"),
                app.Configuration.GetValue<string>("BroadcastSecret"),
                app.Configuration.GetValue<string>("BroadcastUrls"),
                wsb: new WebServerBuilder
                {
                    WebApplication = app,
                    AuthTokenEncryptionKey = "IMPORTANT SECRET FROM Southwind. CHANGE THIS STRING!!!",
                    MachineName = app.Configuration.GetValue<string?>("ServerName"),
                    DefaultCulture = CultureInfo.GetCultureInfo("en")
                });

            Statics.SessionFactory = new ScopeSessionFactory(new VoidSessionFactory());

            log.Switch("UseEndpoints");

            //Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("../swagger/v1/swagger.json", "Southwind API");
            });//Swagger Configure

            app.UseWhen(req => req.Request.Path.StartsWithSegments("/api/reflection/types"), builder =>
            {
                builder.UseResponseCompression();
            });

            app.UseRouting();
#pragma warning disable ASP0014 // Suggest using top level route registrations
            app.UseEndpoints(endpoints =>
            {
                AlertsServer.MapAlertsHub(endpoints);
                ConcurrentUserServer.MapConcurrentUserHub(endpoints);
                endpoints.MapControllers();
                endpoints.MapControllerRoute(
                    name: "spa-fallback",
                    pattern: "{*url}",
                    constraints: new { url = new NoAPIContraint() },
                    defaults: new { controller = "Home", action = "Index" });
            });
#pragma warning restore ASP0014 // Suggest using top level route registrations
        }

        SignumInitializeFilterAttribute.InitializeDatabase = () =>
        {
            using (HeavyProfiler.Log("Startup"))
            using (var log = HeavyProfiler.Log("Initial"))
            {
                log.Switch("Initialize");
                using (AuthLogic.Disable())
                    Schema.Current.Initialize();

                if (app.Configuration.GetValue<bool>("StartBackgroundProcesses"))
                {
                    log.Switch("StartRunningProcesses");
                    ProcessRunner.StartRunningProcessesAfter(5 * 1000);

                    log.Switch("StartScheduledTasks");
                    ScheduleTaskRunner.StartScheduledTaskAfter(5 * 1000);

                    log.Switch("StartRunningEmailSenderAsync");
                    AsyncEmailSender.StartAsyncEmailSenderAfter(5 * 1000);
                }

                SystemEventServer.LogStartStop(app, app.Lifetime);
                
            }
        };

        app.Run();
    }

    class NoAPIContraint : IRouteConstraint
    {
        public bool Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
            var url = (string?)values[routeKey];

            if (url != null && url.StartsWith("api/"))
                return false;

            return true;
        }
    }
}
