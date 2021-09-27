using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace HostedPowershell
{
    public static class Extensions
    {
        public static IServiceCollection AddPowershellBackgroundService(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {

            return AddPowershellBackgroundService(services, configuration, environment.ContentRootFileProvider);
            
        }
    
        public static IServiceCollection AddPowershellBackgroundService(this IServiceCollection services, IConfiguration configuration,IFileProvider fileProvider )
        {
            var sect = configuration.GetSection("BSPWSH");
            services.Configure<BSPWSHOptions>(sect);
            services.AddSingleton<IFileProvider>(sp => fileProvider);
            services.AddHostedService<BSPWSH>();
            return services;
        }
    }
}
          
    