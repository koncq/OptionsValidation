# Koncq.OptionsValidation

Simple generator to register and validate with data annotations the options in .NET projects.

## Usage

It is enough to use extension method `RegisterOptions` like so:

```
services.RegisterOptions(configuration);
```

and mark any options class:

```
[ValidateOptions(SectionName = "Database")]
public class DatabaseOptions
{
    [Range(1, 10)] public int Retries { get; set; }
}
```

This will generate following code:
```
// <auto-generated>
//     Automatically generated by Koncq.OptionsValidation.
//     Changes made to this file may be lost and may cause undesirable behaviour.
// </auto-generated>
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Koncq.OptionsValidation 
{
    public static partial class OptionsValidationExtension
    {
        public static IServiceCollection RegisterOptions(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection
                .AddOptions<DatabaseOptions>()
                .Bind(configuration.GetSection("Database"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            return serviceCollection;
        }
    }
}
```

Attribute `ValidateOptions` has following properties:

- SectionName - defines custom `section name` to retrieve configuration section. Default value is options class name  
- SkipStartupValidation - disables options validation during startup and can lead to exceptions during runtime when value of the property guarded by data annotation does not met the criteria (default: false)

