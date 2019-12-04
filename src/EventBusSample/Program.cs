using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using EventBus;
using EventBus.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        }

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.AddSingleton(context.Configuration);

            services.AddSingleton<IEventStore, EventStoreInMemory>();
            services.AddSingleton<IEventBus, DefaultEventBus>();

            services.AddSingleton<CounterEventHandler1>((@event =>
                    LogHelper.Log(typeof(DelegateEventHandler<CounterEvent>))
                        .Info($"Event Info: {@event.ToJson()}")
                )
            );
        }
    }
}
