namespace Koncq.OptionsValidation.Generator.Tests;

[UsesVerify] // 👈 Adds hooks for Verify into XUnit
public class GeneratorSnapshotTests
{
    [Fact]
    public Task Generates_NoParams_ShouldSucceed()
    {
        // The source code to test
        var source = @"
using Koncq.OptionsValidation.Generator;

[ValidateOptions]
public class Example
{
}";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }
    
    [Fact]
    public Task Generates_WithSectionName_ShouldSucceed()
    {
        // The source code to test
        var source = @"
using Koncq.OptionsValidation.Generator;

[ValidateOptions(SectionName = ""Database"")]
public class Example
{
}";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }
    
    [Fact]
    public Task Generates_WithSkipStartupValidation_ShouldSucceed()
    {
        // The source code to test
        var source = @"
using Koncq.OptionsValidation.Generator;

[ValidateOptions(SkipStartupValidation = true)]
public class Example
{
}";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }
}