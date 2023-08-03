using System.Runtime.CompilerServices;

namespace Koncq.OptionsValidation.Generator.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Initialize();
    }
}