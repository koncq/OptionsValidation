using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Koncq.OptionsValidation.Generator;

[Generator]
public class OptionsValidationGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Add the marker attribute to the compilation
        context.RegisterPostInitializationOutput(ctx =>
            ctx.AddSource(
                $"ValidateOptionsAttribute.g.cs",
                SourceText.From(SourceGenerationHelper.GenerateAttribute(), Encoding.UTF8)));

        // Do a simple filter for enums
        IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s), // select classes with attributes
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx)) // sect the class with the [ValidateOptions] attribute
            .Where(static m => m is not null)!; // filter out attributed class that we don't care about

        // Combine the selected classes with the `Compilation`
        IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses
            = context.CompilationProvider.Combine(classDeclarations.Collect());

        // Generate the source using the compilation and classes
        context.RegisterSourceOutput(compilationAndClasses, static (spc, source) => Execute(source.Item1, source.Item2, spc));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };

    private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        // we know the node is a ClassDeclarationSyntax thanks to IsSyntaxTargetForGeneration
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

        // loop through all the attributes on the method
        foreach (var attributeListSyntax in classDeclarationSyntax.AttributeLists)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                var fullName = attributeContainingTypeSymbol.ToDisplayString();

                // Is the attribute the [ValidateOptionsAttribute] attribute?
                if (fullName == "Koncq.OptionsValidation.Generator.ValidateOptionsAttribute")
                {
                    return classDeclarationSyntax;
                }
            }
        }

        // we didn't find the attribute we were looking for
        return null;
    }

    static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
        {
            return;
        }

        // I'm not sure if this is actually necessary, but `[LoggerMessage]` does it, so seems like a good idea!
        var distinctClasses = classes.Distinct();

        // Convert each ClassDeclarationSyntax to an RegistrationToGenerate
        var enumsToGenerate = GetTypesToGenerate(compilation, distinctClasses, context.CancellationToken);

        // If there were errors in the ClassDeclarationSyntax, we won't create an
        // RegistrationToGenerate for it, so make sure we have something to generate
        if (enumsToGenerate.Count <= 0)
        {
            return;
        }
        
        // generate the source code and add it to the output
        var result = SourceGenerationHelper.GenerateExtensionClass(enumsToGenerate);
        context.AddSource("OptionsValidationExtensions.g.cs", SourceText.From(result, Encoding.UTF8));
    }

    private static List<RegistrationToGenerate> GetTypesToGenerate(Compilation compilation, IEnumerable<ClassDeclarationSyntax> classes, CancellationToken cancellationToken)
    {
        var enumsToGenerate = new List<RegistrationToGenerate>();

        // Get the semantic representation of our marker attribute 
        var classAttribute = compilation.GetTypeByMetadataName("Koncq.OptionsValidation.Generator.ValidateOptionsAttribute");
        if (classAttribute == null)
        {
            // If this is null, the compilation couldn't find the marker attribute type
            // which suggests there's something very wrong! Bail out..
            return enumsToGenerate;
        }

        foreach (var classDeclarationSyntax in classes)
        {
            // initial values for attribute arguments
            string? sectionName = null;
            var skipStartupValidation = false;

            // stop if we're asked to
            cancellationToken.ThrowIfCancellationRequested();

            // Get the semantic representation of the enum syntax
            var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol)
            {
                // something went wrong, bail out
                continue;
            }

            foreach (var attributeData in classSymbol.GetAttributes())
            {
                if (!classAttribute.Equals(attributeData.AttributeClass, SymbolEqualityComparer.Default))
                {
                    continue;
                }

                // This is the attribute, check all of the named arguments
                foreach (var namedArgument in attributeData.NamedArguments)
                {
                    // Is this the SectionName argument?
                    if (namedArgument is { Key: "SectionName", Value.Value: not null })
                    {
                        sectionName = namedArgument.Value.Value.ToString();
                    }

                    // Is this the SectionName argument?
                    if (namedArgument is { Key: "SkipStartupValidation", Value.Value: not null })
                    {
                        skipStartupValidation = (bool)namedArgument.Value.Value;
                    }
                }

                break;
            }


            // Get the full type name of the enum e.g. Colour, 
            // or OuterClass<T>.Colour if it was nested in a generic type (for example)
            var className = classSymbol.ToString();

            // Create an EnumToGenerate for use in the generation phase
            enumsToGenerate.Add(new RegistrationToGenerate
            {
                Name = className,
                SectionName = sectionName,
                SkipStartupValidation = skipStartupValidation
            });
        }

        return enumsToGenerate;
    }
}