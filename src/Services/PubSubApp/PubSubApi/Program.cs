using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace PubSubApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel(serverOptions =>
                    {
                        serverOptions.AddServerHeader = false;
                        //serverOptions.AllowSynchronousIO = false;
                    });
                    webBuilder.UseStartup<Startup>();
                    webBuilder.CaptureStartupErrors(captureStartupErrors: true);
                    webBuilder.UseSetting(WebHostDefaults.PreventHostingStartupKey, "true")
                    //webBuilder.UseIISIntegration()
                    .UseStaticWebAssets();
                });
    }
}
