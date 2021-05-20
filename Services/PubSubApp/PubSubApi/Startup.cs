using Amazon.SQS;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AwsSqsService;
using AzureMessageBus;
using GooglePubSub;
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
using PubSubApi.Infrastructure.IntegrationEvents;
using PubSubApi.Infrastructure.MessageBusSettings;
using System;
using System.IO;
using System.Reflection;

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
                //.AddApplicationsIOptions(Configuration)
                .AddIntegrationsServices(Configuration)
                .RegisterServiceBus(Configuration);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "PubSubApi", Version = "v1" });
                c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{typeof(Startup).GetTypeInfo().Assembly.GetName().Name}.xml"), true);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            this.AutofacContainer = app.ApplicationServices.GetAutofacRoot();
            //CustomAppExtensions.ConfigureEventBus(app);

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

        /// <summary>
        /// Adds up integration Services dependencies
        /// </summary>
        /// <param name="services"></param>
        /// <param name="_config"></param>
        /// <returns></returns>
        public static IServiceCollection AddIntegrationsServices(this IServiceCollection services,
            IConfiguration _config)
        {
            // Azure Message Bus dependencies 
            services.AddSingleton<IServiceBusPersisterConnection>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<ServiceBusPersisterConnection>>();

                var serviceBusConnectionString = _config.GetSection("ServiceBusSettings:ConnectionString");
                var serviceBusConnection = new ServiceBusConnectionStringBuilder(serviceBusConnectionString.Value);
                return new ServiceBusPersisterConnection(serviceBusConnection, logger, string.Empty);
            });

            // GCP Pub/Sub dependencies
            services.AddSingleton<IPubSubPersisterConnection>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<PubSubPersisterConnection>>();
                return new PubSubPersisterConnection(logger, _config);
            });

            // AWS SQS dependencies
            services.AddDefaultAWSOptions(_config.GetAWSOptions());
            services.AddAWSService<IAmazonSQS>();

            services.AddSingleton<ISqsPersisterConnection>(opts =>
            {
                var logger = opts.GetRequiredService<ILogger<SqsPersisterConnection>>();
                return new SqsPersisterConnection(logger, _config);
            });

            return services;
        }

        public static IServiceCollection AddApplicationsIOptions(this IServiceCollection services,
            IConfiguration _config)
        {
            services.Configure<ServiceBusSettings>(options => _config.GetSection(nameof(ServiceBusSettings)).Bind(options));
            services.Configure<GCPPubSubSettings>(options => _config.GetSection(nameof(GCPPubSubSettings)).Bind(options));
            return services;
        }

        /// <summary>
        /// Register SQS, Pub/Sub, Azure Service Bus dependencies.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="_config"></param>
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

            services.AddSingleton<IGcpPubSub, EventBusPubSub>(sp =>
            {
                var pubsubPersisterConnection = sp.GetRequiredService<IPubSubPersisterConnection>();
                var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                var logger = sp.GetRequiredService<ILogger<EventBusPubSub>>();
                var eventBusSubcriptionsManager = sp.GetRequiredService<IEventBusSubscriptionsManager>();

                return new EventBusPubSub(pubsubPersisterConnection, eventBusSubcriptionsManager, iLifetimeScope, logger, _config);
            });

            services.AddSingleton<IAwsSqsQueue, EventBusSqs>(sp =>
            {
                var pubsubPersisterConnection = sp.GetRequiredService<ISqsPersisterConnection>();
                var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                var logger = sp.GetRequiredService<ILogger<EventBusSqs>>();
                var eventBusSubcriptionsManager = sp.GetRequiredService<IEventBusSubscriptionsManager>();


                return new EventBusSqs(pubsubPersisterConnection, logger, eventBusSubcriptionsManager, iLifetimeScope, _config, _config.GetAWSOptions().CreateServiceClient<IAmazonSQS>());
            });

            services.AddSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionsManager>();

            // Register Messagehandlers
            services.AddTransient<ServiceBusMessageEventHandler>();
            services.AddTransient<PubSubMessageEventHandler>();
            services.AddTransient<SqsMessageEventHandler>();

            return services;
        }

        public static void ConfigureEventBus(IApplicationBuilder app)
        {
            var eventBus = app.ApplicationServices.GetRequiredService<IAwsSqsQueue>();

            #region Configure Event Bus

            eventBus.SubscriberCreateSqs<PublishMessageEvent, SqsMessageEventHandler>(string.Empty);

            #endregion Configure Event Bus
        }

        #endregion Configure Services for App
    }
}
