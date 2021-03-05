# Introduction into Azure App Configuration
This repository shows, how to integration Azure App Configuration into an ASP.Net API.

## Benefits of App Configuration in your application

Azure App Configuration lets you change the settings of your application while deployed. No matter if those settings are features you want to disable or if you want to change app settings on the fly, app configuration helps you achieve this. 
In addition to managing your application remotely, Azure App Configuration supports updating multiple Stages from one place:
If you are developing an application that has different settings depending on the deployment stage, you can use so called labels to set different settings for different stages.

It is really simple to incorporate Azure App Configuration into any .Net application. This repo focuses on an ASP.Net Core API as an example but you can find many examples provided by Microsoft.

## Prerequisites

To follow this guide, you need an Azure Subscription to be able to create an resource named "App Configuration" (create one for free [here](https://azure.microsoft.com/free/dotnet)) and [Visual Studio 2019](https://visualstudio.microsoft.com/de/downloads/).

## Getting Started

### 1. Create the resource "App Configuration" 

- Go to [Azure Portal](https://portal.azure.com/#home)
- Press "Create a resource"
- Search for "App Configuration"
- Click on the first resource type that appears
- Press Create on the Details screen
- Fill in your details (__Remark:__ you might want to opt for the free tier if you can)
- Press "Review + Create" 
- Confirm your new resource request with "Create"
- After the resource has been created, navigate to it.
- Click on "Access keys" (left menu) 
- Copy your primary "Connection string" (this will go into the appsettings of your application later)

### 2. Add your first app configuration settings:

- Go to your new Azure resource of type "App Configuration"
- Navigate to the section "Operations" -> "Configuration explorer"
- Press Create and then on "Key-value" 
- Enter as Key: "AppConfiguration:ChangeThisToUpdate", Value: 0 and Label: "Development" (Content-Type can be left empty).
- Press Apply
- Add another Key-value pair: Key: "SomeRandomConfiguration", Value: "test-value", Label: "Development" and Content-Type empty

__Optional steps:__
To add another label to your value, right click some key, click "Add value", enter another value and add a new label.

### 3. Create a C# project 
(we will use an ASP.Net Core Rest API for demonstration purposes) 

- Open Visual Studio 2019
- Press "Create a new project"
- Search for "ASP.NET Core Web Application"
- Give it a name and a location
- Confirm with Create
- Now configure your ASP.Net Web Application as "ASP.Net Core Web API" (you can omit the "Configure for HTTPS"-option)
- Press create

### 4. Integrate "App Configuration" into application

- Install the NuGet package: "Microsoft.Azure.AppConfiguration.AspNetCore" (at the time of writing version 4.1.0)
- Modify "appsettings.json":
  - Replace the contents of this file with:

  ```json
  {
    "AppConfiguration": {
      "ConnectionString": "<Your-App-Configuration-ConnectionString>",
      "Label": "Development"
    }
  }
  ```
  __Important note:__ Please make sure not to commit or push your connection string into a repo. The connection string should be saved as an environment variable on the executing machine. We do this here only for demo purposes. (See [here](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-5.0#environment-variables) for more details)

- Modify Program.cs:
  - Add a new method:
    ```cs
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
    ```
  - Replace the CreateHostBuilder method with this new one:
    __Remark:__ You might need to add this using: ```using Microsoft.Extensions.Configuration.AzureAppConfiguration;```
    
    ```cs
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
    ```

- Modify Startup.cs:

  - Change your constructor to take in an IConfiguration instance (We need to register this instance in order to inject it into a controller for demo purposes)

    ```cs
    public Microsoft.Extensions.Configuration.IConfiguration Configuration { get; }

    public Startup(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        Configuration = configuration;
    }
    ```

  - Modify "ConfigureServices" to register this instance:

    ```cs
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(Configuration);
    }
    ```

- Create a new Controller in the "Controllers" folder of your project named "SettingsController":
  ```cs
  [ApiController]
  [Route("[controller]")]
  public class SettingsController : Microsoft.AspNetCore.Mvc.ControllerBase
  {
      private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

      public SettingsController(Microsoft.Extensions.Configuration.IConfiguration configuration)
      {
          _configuration = configuration;
      }

      [HttpGet]
      public string Get()
      {
          return _configuration["SomeRandomConfiguration"]; // Make sure this key is registered in app configuration
      }
  }
  ```

### 5. Test your setup

If you now run your API (preferably not using IIS), your browser should open. If you then navigate to [https://localhost:5001/settings](https://localhost:5001/settings) you should see the value you have entered into App Configuration. Congratulations!
If you now change the value for Key "SomeRandomConfiguration" and the label "Development" in App Configuration and confirm your change by modifing the value of the setting "AppConfiguration:ChangeThisToUpdate" for the same label, the Api updates its configuration within the next 30 seconds and should then display the updated value when you reload your [page](https://localhost:5001/settings).


## Further reading

1. Learn about Feature Toggles in App Configuration to enable and disable certain features of you application: [Introduction to feature flags](https://docs.microsoft.com/en-us/azure/azure-app-configuration/concept-feature-management) and [ASP.NET Core integration](https://docs.microsoft.com/en-us/azure/azure-app-configuration/quickstart-feature-flag-aspnet-core)
2. Azure App Configuration FAQ: [here](https://docs.microsoft.com/en-us/azure/azure-app-configuration/faq)
3. Azure App Configuration using CLI: [here](https://docs.microsoft.com/en-us/azure/azure-app-configuration/cli-samples)
