namespace Koncq.OptionsValidation.Generator;

public class RegistrationToGenerate : Attribute
{
    public string Name { get; set; } = string.Empty;
    public string? SectionName { get; set; }
    public bool SkipStartupValidation { get; set; }
}