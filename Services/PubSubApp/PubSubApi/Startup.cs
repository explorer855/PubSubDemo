using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureMessageBus;
using MessageBusCore;
using MessageBusCore.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PubSubApi.Infrastructure.MessageBusSettings;
using PubSubApi.Infrastructure.Models.IntegrationEvents;

namespace PubSubApi
{
    public class Startup
    {
        public ILifetimeScope AutofacContainer { get; private set; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddMvcCoreService()
                .AddSessionsService()
                .AddApplicationsIOptions(Configuration)
                .AddIntegrationsServices(Configuration)
                .RegisterServiceBus(Configuration);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "PubSubApi", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            this.AutofacContainer = app.ApplicationServices.GetAutofacRoot();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PubSubApi v1"));
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }

    internal static class CustomAppExtensions
    {
        #region Configure Services for App

        public static IServiceCollection AddMvcCoreService(this IServiceCollection services)
        {
            services.AddControllers()
                .AddNewtonsoftJson(opts =>
                {
                    opts.SerializerSettings.ContractResolver = new DefaultContractResolver();
                    opts.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    opts.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
                    opts.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    opts.SerializerSettings.StringEscapeHandling = StringEscapeHandling.EscapeHtml;
                });

            services.AddMvcCore(opt =>
            {
                opt.OutputFormatters.RemoveType<TextOutputFormatter>();
                opt.OutputFormatters.RemoveType<StringOutputFormatter>();
                opt.OutputFormatters.RemoveType<HttpNoContentOutputFormatter>();
            })
                .AddApiExplorer()
                .AddDataAnnotations() // Always keep it in config for Model Validation
                .AddMvcOptions(opts =>
                {
                    opts.RespectBrowserAcceptHeader = true;
                    opts.ReturnHttpNotAcceptable = true;
                });

            return services;
        }

        public static IServiceCollection AddSessionsService(this IServiceCollection services)
        {
            #region Configure Session

            services.AddHttpContextAccessor();
            services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                // Set a short timeout for easy testing.
                //options.IdleTimeout = TimeSpan.FromHours(1);
                options.Cookie.HttpOnly = true;
                // Make the session cookie essential
                options.Cookie.IsEssential = true;
            });

            #endregion Configure Session

            return services;
        }

        public static IServiceCollection AddIntegrationsServices(this IServiceCollection services,
            IConfiguration _config)
        {
            //services.AddTransient<IEventService, EventService>();

            services.AddSingleton<IServiceBusPersisterConnection>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<ServiceBusPersisterConnection>>();

                var serviceBusConnectionString = _config.GetSection("ServiceBusSettings:ConnectionString");
                var serviceBusConnection = new ServiceBusConnectionStringBuilder(serviceBusConnectionString.Value);
                //var subscriptionClientName = _config["SubscriptionClient"];

                return new ServiceBusPersisterConnection(serviceBusConnection, logger, string.Empty);
            });

            return services;
        }

        public static IServiceCollection AddApplicationsIOptions(this IServiceCollection services,
            IConfiguration _config)
        {
            services.Configure<ServiceBusSettings>(options => _config.GetSection(nameof(ServiceBusSettings)).Bind(options));
            return services;
        }

        public static IServiceCollection RegisterServiceBus(this IServiceCollection services,
            IConfiguration _config)
        {
            services.AddSingleton<IEventBus, EventBusServiceBus>(sp =>
            {
                var serviceBusPersisterConnection = sp.GetRequiredService<IServiceBusPersisterConnection>();
                var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                var logger = sp.GetRequiredService<ILogger<EventBusServiceBus>>();
                var eventBusSubcriptionsManager = sp.GetRequiredService<IEventBusSubscriptionsManager>();

                return new EventBusServiceBus(serviceBusPersisterConnection, logger,
                    eventBusSubcriptionsManager, _config["SubscriptionClient"], iLifetimeScope, _config);
            });

            services.AddSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionsManager>();

            // Register Messagehandlers
            services.AddTransient<PublishMessageEventHandler>();

            return services;
        }

        #endregion Configure Services for App
    }
}
