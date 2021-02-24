using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Hosting;

namespace RestApi
{
    public class Program
    {
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                    webBuilder.ConfigureAppConfiguration(config =>
                    {
                        var settings = config.Build();

                        // read connection details from current configuration
                        // (env. variables or user secrets should be used here!
                        var connectionString = settings.GetSection("AppConfiguration")["ConnectionString"];
                        var label = settings.GetSection("AppConfiguration")["Label"];

                        config.AddAzureAppConfiguration(options =>
                        {
                            // connect to Azure App Configuration but only take values with the environment label
                            options.Connect(connectionString)
                                .ConfigureRefresh(opt =>
                                {
                                    // Register a key in azure app configuration that when it is changed will update all loaded configurtations
                                    opt.Register("AppConfiguration:ChangeThisToUpdate", label, refreshAll: true)
                                      .SetCacheExpiration(TimeSpan.FromSeconds(30));
                                })

                                // filter only for keys under a given label
                                // (in this case a string we can define in the configuration outside of app configuration)
                                .Select(KeyFilter.Any, label)
                                .UseFeatureFlags(opt =>
                                {
                                    opt.Label = label;
                                    opt.CacheExpirationInterval = TimeSpan.FromSeconds(30);
                                });

                            // create a function that will automatically refresh every 30 seconds to look for changes in "AppConfiguration:ChangeThisToUpdate"
                            StartRefreshingRegularly(options.GetRefresher());
                        }, optional: false);
                    })
                    .UseStartup<Startup>());
        }

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Starts a Task that runs indefinitely and regulary refreshes all stored configuration from Azure App configuration
        /// </summary>
        private static void StartRefreshingRegularly(IConfigurationRefresher configurationRefresher)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await Task.Delay(30_000).ConfigureAwait(false);
                        var result = await configurationRefresher.TryRefreshAsync().ConfigureAwait(false);
                        if (result)
                        {
                            Console.WriteLine("Refreshed configuration from Azure App Configuration.");
                        }
                        else
                        {
                            Console.WriteLine("Could not refresh configuration from Azure App Configuration.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex);
                    }
                }
            });
        }
    }
}