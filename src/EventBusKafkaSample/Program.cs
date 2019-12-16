using System;
using System.Runtime.Serialization;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Confluent.Kafka;
using EventBus;
using EventBus.Abstractions;
using EventBus.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventBusKafkaSample
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = CreateHostBuilder(args);
            host.Run();
        }

        private static IHost CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
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
                .ConfigureServices(ConfigureServices)
                .Build(); //提供通用注入配置

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            var configuration = context.Configuration;

            services.AddHostedService<EventService>();

            services.AddSingleton<DefaultKafkaConsumerConnection<Null, byte[]>>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<DefaultKafkaConsumerConnection<Null, byte[]>>>();

                var config = new ConsumerConfig
                {
                    BootstrapServers = configuration["EventBusConnection"],
                    GroupId = configuration["SubscriptionClientName"],
                    EnableAutoCommit = false,
                    StatisticsIntervalMs = 5000,
                    SessionTimeoutMs = 6000,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    EnablePartitionEof = true
                };

                if (!string.IsNullOrEmpty(configuration["EventBusUserName"]))
                {
                    config.SaslUsername = configuration["EventBusUserName"];
                }

                if (!string.IsNullOrEmpty(configuration["EventBusPassword"]))
                {
                    config.SaslPassword = configuration["EventBusPassword"];
                }

                var retryCount = 5;
                if (!string.IsNullOrEmpty(configuration["EventBusRetryCount"]))
                {
                    retryCount = int.Parse(configuration["EventBusRetryCount"]);
                }

                return new DefaultKafkaConsumerConnection<Null, byte[]>(config, logger, retryCount);
            });

            services.AddSingleton<DefaultKafkaProducerConnection<Null, byte[]>>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<DefaultKafkaProducerConnection<Null, byte[]>>>();

                var config = new ProducerConfig
                {
                    BootstrapServers = configuration["EventBusConnection"],
                    StatisticsIntervalMs = 5000,
                };

                if (!string.IsNullOrEmpty(configuration["EventBusUserName"]))
                {
                    config.SaslUsername = configuration["EventBusUserName"];
                }

                if (!string.IsNullOrEmpty(configuration["EventBusPassword"]))
                {
                    config.SaslPassword = configuration["EventBusPassword"];
                }

                var retryCount = 5;
                if (!string.IsNullOrEmpty(configuration["EventBusRetryCount"]))
                {
                    retryCount = int.Parse(configuration["EventBusRetryCount"]);
                }

                return new DefaultKafkaProducerConnection<Null, byte[]>(config, logger, retryCount);
            });

            RegisterEventBus(context, services);
        }


        private static void RegisterEventBus(HostBuilderContext context, IServiceCollection services)
        {
            var configuration = context.Configuration;

            services.AddSingleton<IEventBus, EventBusKafka>(sp =>
            {
                var consumerConnection = sp.GetRequiredService<DefaultKafkaConsumerConnection<Null, byte[]>>();
                var producerConnection = sp.GetRequiredService<DefaultKafkaProducerConnection<Null, byte[]>>();
                var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                var logger = sp.GetRequiredService<ILogger<EventBusKafka>>();
                var eventStore = sp.GetRequiredService<IEventStore>();

                var retryCount = 5;
                if (!string.IsNullOrEmpty(configuration["EventBusRetryCount"]))
                {
                    retryCount = int.Parse(configuration["EventBusRetryCount"]);
                }

                return new EventBusKafka(producerConnection, consumerConnection, eventStore, logger, iLifetimeScope, retryCount);
            });

            services.AddSingleton<IEventStore, EventStoreInMemory>();

            services.AddTransient<CounterEventHandler1>();
            services.AddTransient<CounterEventHandler2>();
            services.AddTransient<CounterEventHandler3>();
        }
    }
}
