using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Autofac;
using EventBus;
using EventBus.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EventBusRabbitMQ;
using RabbitMQ.Client;

namespace EventBusSample
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = new HostBuilder()
                .UseConsoleLifetime() //使用控制台生命周期
                .ConfigureHostConfiguration(configurationBinder =>
                {
                    configurationBinder
                        .AddEnvironmentVariables()
                        .AddCommandLine(args);
                })
                .ConfigureAppConfiguration((context, configurationBuilder) =>
                {
                    var env = context.HostingEnvironment;

                    configurationBuilder
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables()
                        .AddCommandLine(args);
                })
                .ConfigureLogging((context, builder) =>
                {
                    builder.AddConfiguration(context.Configuration.GetSection("Logging"));
                    builder.AddConsole();
                    builder.AddDebug();
                }) //注入日志组件
                .ConfigureServices(ConfigureServices)//提供通用注入配置
                .Build();

            new AutofacServiceProvider(host);
        }

        private static void RegisterEventBus(HostBuilderContext context, IServiceCollection services)
        {
            var Configuration = context.Configuration;

            var subscriptionClientName = Configuration["SubscriptionClientName"];

            services.AddSingleton<IEventBus, EventBusRabbitMQ.EventBusRabbitMQ>(sp =>
            {
                var rabbitMQConnection = sp.GetRequiredService<IRabbitMQConnection>();
                var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                var logger = sp.GetRequiredService<ILogger<EventBusRabbitMQ.EventBusRabbitMQ>>();
                var eventStore = sp.GetRequiredService<IEventStore>();

                var retryCount = 5;
                if (!string.IsNullOrEmpty(Configuration["EventBusRetryCount"]))
                {
                    retryCount = int.Parse(Configuration["EventBusRetryCount"]);
                }

                return new EventBusRabbitMQ.EventBusRabbitMQ(rabbitMQConnection, eventStore, logger, iLifetimeScope, subscriptionClientName, retryCount);
            });

            services.AddSingleton<IEventStore, EventStoreInMemory>();

            services.AddTransient<CounterEventHandler1>();
            services.AddTransient<CounterEventHandler2>();

        }

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            var Configuration = context.Configuration;

            services.AddSingleton(context.Configuration);

            services.AddSingleton<IRabbitMQConnection>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<DefaultRabbitMQConnection>>();

                var factory = new ConnectionFactory()
                {
                    HostName = Configuration["EventBusConnection"],
                    DispatchConsumersAsync = true
                };

                if (!string.IsNullOrEmpty(Configuration["EventBusUserName"]))
                {
                    factory.UserName = Configuration["EventBusUserName"];
                }

                if (!string.IsNullOrEmpty(Configuration["EventBusPassword"]))
                {
                    factory.Password = Configuration["EventBusPassword"];
                }

                var retryCount = 5;
                if (!string.IsNullOrEmpty(Configuration["EventBusRetryCount"]))
                {
                    retryCount = int.Parse(Configuration["EventBusRetryCount"]);
                }

                return new DefaultRabbitMQConnection(factory, logger, retryCount);
            });

            RegisterEventBus(context, services);

            ConfigureEventBus(services);

            
        }

        private static void ConfigureEventBus(IServiceCollection services)
        {
            var eventBus = services.BuildServiceProvider().GetRequiredService<IEventBus>();

            eventBus.Subscribe<CounterEvent, CounterEventHandler1>();
            eventBus.Subscribe<CounterEvent, CounterEventHandler2>();
        }
    }
}
