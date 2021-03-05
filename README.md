# Introduction into Azure App Configuration
This repository shows, how to integration Azure App Configuration into an ASP.Net API.

## Benefits of App Configuration in your application

Azure App Configuration lets you change the settings of your application while deployed. No matter if those settings are features you want to disable or if you want to change app settings on the fly, app configuration helps you achieve this. 
In addition to managing your application remotely, Azure App Configuration supports updating multiple Stages from one place:
If you are developing an application that has different settings depending on the deployment stage, you can use so called labels to set different settings for different stages.

It is really simple to incorporate Azure App Configuration into any .Net application. This repo focuses on an ASP.Net Core API as an example but you can find many examples provided by Microsoft.

## Prerequisites

To follow this guide, you need an Azure Subscription to be able to create an resource named "App Configuration".

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

### 2. Create a C# project 
(we will use an ASP.Net Core Rest API for demonstration purposes) 

- Open Visual Studio 2019
- Press "Create a new project"
- Search for "ASP.NET Core Web Application"
- Give it a name and a location
- Confirm with Create
- Now configure your ASP.Net Web Application as "ASP.Net Core Web API" (you can omit the "Configure for HTTPS"-option)
- Press create

### 3. Integrate "App Configuration" into application

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
  __Important note:__ Please make sure not to commit or push your connection string into a repo. The connection string should be saved as an environment variable on the executing machine. We do this here only for demo purposes. (See [here](https://docs.microsoft.com/de-de/aspnet/core/fundamentals/configuration/?view=aspnetcore-5.0#environment-variables) for more details)

- Modify Program.cs:

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


## Further reading

