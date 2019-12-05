using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace EventBusSample
{
    public class Startup
    {
        public virtual IServiceProvider ConfigureServices(IServiceCollection services)
        {
            var container = new ContainerBuilder();
            container.Populate(services);

            return new AutofacServiceProvider(container.Build());
        }
    }
}
