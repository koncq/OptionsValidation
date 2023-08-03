﻿using System.Text;

namespace Koncq.OptionsValidation.Generator;

public static class SourceGenerationHelper
{
    private const string AutoGeneratedHeader = @"// <auto-generated>
//     Automatically generated by Koncq.OptionsValidation.
//     Changes made to this file may be lost and may cause undesirable behaviour.
// </auto-generated>";

    public static string Attribute()
    {
        var sb = new StringBuilder();
        sb.Append(AutoGeneratedHeader);
        sb.Append(
        @"
#nullable enable

namespace Koncq.OptionsValidation.Generator
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class ValidateOptionsAttribute : System.Attribute
    {
        public string? SectionName { get; set; }
        public bool SkipStartupValidation { get; set; }
    }
}");

        return sb.ToString();
    }


    public static string GenerateExtensionClass(List<RegistrationToGenerate> classesToGenerate)
    {
        var sb = new StringBuilder();
        sb.Append(AutoGeneratedHeader);
        sb.Append(@"
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Koncq.OptionsValidation 
{
    public static partial class OptionsValidationExtension
    {
        public static IServiceCollection RegisterOptions(this IServiceCollection serviceCollection, IConfiguration configuration)
        {");

        foreach (var toGenerate in classesToGenerate)
        {
            sb.Append(GenerateOptionsRegistration(toGenerate)).AppendLine();
        }

        sb.Append(@"
            return serviceCollection;
        }
    }
}");

        return sb.ToString();
    }

    private static string GenerateOptionsRegistration(RegistrationToGenerate registrationToGenerate)
    {
        var sb = new StringBuilder();
        sb.Append($@"
            serviceCollection
                .AddOptions<{registrationToGenerate.Name}>()
                .Bind(configuration.GetSection({GetSectionName(registrationToGenerate)}))
                .ValidateDataAnnotations()");

        if (!registrationToGenerate.SkipStartupValidation)
        {
            sb.Append(@"
                .ValidateOnStart()");
        }

        return sb.Append(";").ToString();
    }

    private static string GetSectionName(RegistrationToGenerate registrationToGenerate)
    {
        var sectionName = registrationToGenerate.SectionName ?? registrationToGenerate.Name;

        sectionName = sectionName.Split('.').Last();

        return string.Concat("\"", $"{sectionName}", "\"");
    }
}