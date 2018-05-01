using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using tradeBot.Jobs;
using tradeBot.lib;
using FluentScheduler;

namespace tradeBot
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(provider => { return Configuration; });
            services.AddSingleton<IRestApi, RestApi>();
            services.AddSingleton<IBot, Bot>();
            services.AddMvc();

        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IRestApi restApi, IBot bot)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Public}/{action=Home}/{id?}");
            });

            JobManager.Initialize(new ApiJobRegistrations(env, restApi));
            JobManager.Initialize(new BotJobRegistrations(env, bot));
            JobManager.JobException += (info) => loggerFactory.CreateLogger("JOB").LogError("An error just happened with a scheduled job: " + info.Exception);
        }
    }
}
