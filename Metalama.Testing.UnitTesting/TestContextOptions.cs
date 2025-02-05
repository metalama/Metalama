// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Reflection;

namespace Metalama.Testing.UnitTesting;

/// <summary>
/// Options that influence the <see cref="UnitTestClass.CreateTestContext(Metalama.Testing.UnitTesting.TestContextOptions?,Metalama.Framework.Engine.Services.IAdditionalServiceCollection?,string?,string?)"/>
/// method.
/// </summary>
[PublicAPI]
public record TestContextOptions
{
    public bool RequiresExclusivity { get; init; }

    /// <summary>
    /// Gets the default <see cref="TestContextOptions"/> value.
    /// </summary>
    public static TestContextOptions Default { get; } = new();

    /// <summary>
    /// Gets the set of MSBuild properties exposed to the tests.
    /// </summary>
    public ImmutableDictionary<string, string> Properties { get; init; } = ImmutableDictionary<string, string>.Empty;

    public GlobalServiceProvider RunnerServiceProvider { get; init; } = (GlobalServiceProvider) ServiceProvider<IGlobalService>.Empty;

    /// <summary>
    /// Gets a value indicating whether the output code should be formatted.
    /// </summary>
    [Obsolete( "Use CodeFormattingOptions instead.", false )]
    public bool FormatOutput
    {
        get => this.CodeFormattingOptions == CodeFormattingOptions.Formatted;
        init
            => _ = value switch
            {
                true => this.CodeFormattingOptions = CodeFormattingOptions.Formatted,
                false => this.CodeFormattingOptions = CodeFormattingOptions.Default
            };
    }

    /// <summary>
    /// Gets code formatting options that indicate how the output code should be formatted.
    /// </summary>
    public CodeFormattingOptions CodeFormattingOptions { get; init; }

    /// <summary>
    /// Gets a value indicating whether the compile-time code should be formatted.
    /// </summary>
    public bool FormatCompileTimeCode { get; init; }

    /// <summary>
    /// Gets the set of assemblies that should be added as references to the compile-time compilation.
    /// </summary>
    public ImmutableArray<Assembly> AdditionalAssemblies { get; init; } = ImmutableArray<Assembly>.Empty;

    /// <summary>
    /// Gets a value indicating whether an error should be reported if all aspect classes
    /// are not strongly ordered.
    /// </summary>
    public bool RequireOrderedAspects { get; init; }

    internal bool HasSourceGeneratorTouchFile { get; init; }

    internal bool HasBuildTouchFile { get; init; }

    internal bool RoslynIsCompileTimeOnly { get; init; } = true;

    /// <summary>
    /// Gets the list of extension types given explicitly as types using <see cref="ITestExtensionCollector"/>,
    /// typically through the unit test framework.
    /// </summary>
    public ImmutableArray<Type> ExtensionTypes { get; init; } = ImmutableArray<Type>.Empty;

    public ImmutableArray<Type> DesignTimeExtensionTypes { get; init; } = ImmutableArray<Type>.Empty;

    /// <summary>
    /// Gets extension assemblies given as paths, typically through the aspect test framework.
    /// </summary>
    internal ImmutableArray<string> ExtensionAssemblies { get; init; } = ImmutableArray<string>.Empty;

    internal ImmutableArray<string> DesignTimeExtensionAssemblies { get; init; } = ImmutableArray<string>.Empty;

    public ImmutableArray<string> CompileTimeAssemblies { get; init; } = ImmutableArray<string>.Empty;

    public ImmutableArray<string> TestPlugInTypes { get; init; } = ImmutableArray<string>.Empty;

    /// <summary>
    /// Gets the list of references that will be added to compilations created in this context.
    /// </summary>
    public ImmutableArray<PortableExecutableReference> AdditionalMetadataReferences { get; init; } = ImmutableArray<PortableExecutableReference>.Empty;

    /// <summary>
    /// Gets the test timeout period, after which the <see cref="TestContext.CancellationToken"/> of the <see cref="TestContext"/> is signalled.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds( 240 );

    public string? ProjectName { get; init; }

    public bool IgnoreUserProfileLicenses { get; init; }

    /// <summary>
    /// Gets the number of characters in the temporary directory.
    /// This allows to reproduce issues linked to the length of the temp path.
    /// </summary>
    public int? TempPathLength { get; init; }
}