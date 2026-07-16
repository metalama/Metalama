// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

/// <summary>
/// In-process (unit-test) reproduction of https://github.com/metalama/Metalama/issues/1710, mirroring the real
/// solution structure that triggers it:
/// <list type="bullet">
/// <item>a <c>Contracts</c>-like shared library that defines a hierarchical option type and a conditionally
/// inheritable aspect (like <c>Metalama.Patterns.Contracts</c>), multi-targeted so its compile-time projection
/// is built per consumer TFM — here the <c>net472</c> and <c>netstandard2.0</c> copies from the crash report;</item>
/// <item>a <c>Library</c> project (netstandard2.0) that references the netstandard2.0 <c>Contracts</c> copy and
/// declares a <c>Base</c> class carrying an inheritable contract;</item>
/// <item>an <c>App</c> project (net472) that references the net472 <c>Contracts</c> copy and <c>Library</c>, and
/// declares <c>Derived : Base</c>.</item>
/// </list>
/// At design time in one shared <c>CompileTimeDomain</c> (as in Rider's single Roslyn worker), both
/// <c>Contracts</c> compile-time copies are loaded. When <c>App</c>'s pipeline collects the inherited contract on
/// <c>Derived</c>, <c>AspectInstance..ctor</c> calls the aspect's <c>IsInheritable</c>, which reads the merged
/// hierarchical options: the <c>Base</c> options (bound to the netstandard2.0 <c>Contracts</c> copy, from
/// <c>Library</c>'s transitive manifest) are merged against <c>App</c>'s default options (bound to the net472
/// copy). <c>ContractOptions.ApplyChanges</c> casts its argument to its own copy's type and throws
/// <see cref="InvalidCastException"/> — the exact production crash.
/// </summary>
public sealed class CrossTfmInheritedOptionsTests : UnitTestClass
{
    public CrossTfmInheritedOptionsTests( ITestOutputHelper testOutput ) : base( testOutput ) { }

    // The shared, multi-targeted library. Compiled once per TFM below; the differing [assembly: TargetFramework]
    // is what forks it into two distinct compile-time projections (ComputeSourceHash folds in the target
    // framework), exactly like the per-TFM compile-time builds shipped in the Contracts package. Note the
    // compile-time code itself is identical across the two TFMs — the copies are distinct purely by TFM identity.
    private static string GetContractsCode( string targetFramework )
        => $$"""
             using System;
             using System.Collections.Generic;
             using Metalama.Framework.Aspects;
             using Metalama.Framework.Code;
             using Metalama.Framework.Options;

             [assembly: System.Runtime.Versioning.TargetFramework("{{targetFramework}}")]

             namespace Contracts
             {
                 public sealed class ContractOptions : IHierarchicalOptions<INamedType>
                 {
                     public bool? IsInheritable { get; init; }

                     public object ApplyChanges( object changes, in ApplyChangesContext context )
                     {
                         var other = (ContractOptions) changes;
                         return new ContractOptions { IsInheritable = other.IsInheritable ?? this.IsInheritable };
                     }

                     public IHierarchicalOptions? GetDefaultOptions( OptionsInitializationContext context )
                         => new ContractOptions();
                 }

                 [AttributeUsage( AttributeTargets.Class, AllowMultiple = true )]
                 public sealed class ConfigureContractAttribute : Attribute, IHierarchicalOptionsProvider
                 {
                     public bool IsInheritable { get; set; }

                     public IEnumerable<IHierarchicalOptions> GetOptions( in OptionsProviderContext context )
                         => new[] { new ContractOptions { IsInheritable = this.IsInheritable } };
                 }

                 // Mirrors ContractBaseAttribute: conditionally inheritable, and the inheritability decision reads
                 // the hierarchical options of the target declaration.
                 public class ContractAspect : TypeAspect, IConditionallyInheritableAspect
                 {
                     bool IConditionallyInheritableAspect.IsInheritable( IDeclaration targetDeclaration, IAspectInstance aspectInstance )
                         => (targetDeclaration as INamedType)?.Enhancements().GetOptions<ContractOptions>().IsInheritable ?? true;
                 }
             }
             """;

    private const string _libraryCode = """
                                         using Contracts;

                                         [ConfigureContract( IsInheritable = true )]
                                         [ContractAspect]
                                         public class Base { }
                                         """;

    private const string _appCode = """
                                     using Contracts;

                                     public class Derived : Base { }
                                     """;

    [Fact]
    public void InheritedContractOptions_AcrossTfmSpecificContractsCopies_DoNotCrash()
    {
        using var testContext = this.CreateTestContext();
        using var libraryContext = this.CreateTestContext();
        using var appContext = this.CreateTestContext();

        // Two compile-time copies of the same shared library, one per consumer TFM.
        var contractsNetStandard = testContext.CreateCSharpCompilation(
            GetContractsCode( ".NETStandard,Version=v2.0" ),
            assemblyName: "Contracts" );

        var contractsNetFramework = testContext.CreateCSharpCompilation(
            GetContractsCode( ".NETFramework,Version=v4.7.2" ),
            assemblyName: "Contracts" );

        // Library (netstandard2.0) references the netstandard2.0 Contracts copy and declares the inheritable contract.
        var library = testContext.CreateCSharpCompilation(
            _libraryCode,
            assemblyName: "Library",
            additionalReferences: new[] { contractsNetStandard.ToMetadataReference() } );

        // App (net472) references the net472 Contracts copy and Library, and derives from Base.
        var app = testContext.CreateCSharpCompilation(
            _appCode,
            assemblyName: "App",
            additionalReferences: new[] { contractsNetFramework.ToMetadataReference(), library.ToMetadataReference() } );

        using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        // Run Library's pipeline first so its transitive manifest (Base's inheritable options + the aspect,
        // bound to the netstandard2.0 Contracts copy) is available to App's pipeline.
        Assert.True( pipelineFactory.TryExecute( libraryContext.ProjectOptions, library, default, out _ ) );

        // Running App's pipeline collects the inherited contract on Derived; deciding its inheritability merges
        // Library's (netstandard2.0-copy) Base options with App's (net472-copy) default options. Before the fix
        // this throws InvalidCastException, unhandled, during AspectInstance construction.
        Exception? thrown = null;
        var success = false;
        ImmutableArray<Diagnostic> diagnostics = default;

        try
        {
            success = pipelineFactory.TryExecute( appContext.ProjectOptions, app, default, out _, out diagnostics );
        }
        catch ( Exception e )
        {
            thrown = e;
        }

        this.TestOutput.WriteLine( $"success={success}, thrown={thrown?.GetType().Name}" );

        if ( thrown != null )
        {
            this.TestOutput.WriteLine( thrown.ToString() );
        }

        if ( !diagnostics.IsDefault )
        {
            foreach ( var d in diagnostics )
            {
                this.TestOutput.WriteLine( d.ToString() );
            }
        }

        var castErrorObserved =
            (thrown != null && Flatten( thrown ).Any( e => e is InvalidCastException ))
            || (!diagnostics.IsDefault && diagnostics.Any( d => d.ToString().Contains( "cannot be cast", StringComparison.Ordinal ) ));

        Assert.False( castErrorObserved, "The cross-TFM ContractOptions merge threw InvalidCastException (issue #1710)." );
    }

    private static IEnumerable<Exception> Flatten( Exception e )
    {
        for ( var current = e; current != null; current = current.InnerException )
        {
            yield return current;

            if ( current is AggregateException aggregate )
            {
                foreach ( var inner in aggregate.InnerExceptions.SelectMany( Flatten ) )
                {
                    yield return inner;
                }
            }
        }
    }
}
