using System.Runtime.CompilerServices;

namespace Koncq.OptionsValidation.Generator.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    [Obsolete("Obsolete")]
    public static void Init()
    {
        VerifySourceGenerators.Enable();
    }
}