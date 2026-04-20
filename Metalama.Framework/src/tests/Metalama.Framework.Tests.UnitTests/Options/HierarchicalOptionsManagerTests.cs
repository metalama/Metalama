// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.HierarchicalOptions;
using Metalama.Framework.Options;
using Metalama.Testing.UnitTesting;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Options;

/// <summary>
/// Tests for <see cref="HierarchicalOptionsManager"/> behavior when option types are unresolvable.
/// Regression tests for https://github.com/metalama/Metalama/issues/659.
/// </summary>
public sealed class HierarchicalOptionsManagerTests : UnitTestClass
{
    [Fact]
    public async Task ExternalProviderWithUnresolvableType()
    {
        // Regression test for #659: When an IExternalHierarchicalOptionsProvider returns an option type
        // that was not registered during initialization (because the type could not be resolved),
        // HierarchicalOptionsManager should not throw AssertionFailedException.

        using var context = this.CreateTestContext();

        var compilationModel = context.CreateCompilationModel( "public class C { }" );

        var manager = new HierarchicalOptionsManager( context.ServiceProvider );

        // Initialize with an empty project (no option types to register) but with an external provider
        // that returns an unresolvable type name. This simulates the scenario where a referenced assembly's
        // manifest lists option types that can't be resolved in the current compilation.
        await manager.InitializeAsync(
            CompileTimeProject.Empty,
            sources: [],
            new UnresolvableExternalOptionsProvider(),
            compilationModel,
            diagnosticSink: null!,
            cancellationToken: default );

        // Verify that GetOptions returns null for an unregistered option type instead of throwing.
        var declaration = compilationModel.Types.Single();
        var options = manager.GetOptions( declaration, typeof(UnresolvableOptions) );

        Assert.Null( options );
    }

    /// <summary>
    /// Mock <see cref="IExternalHierarchicalOptionsProvider"/> that returns a type name
    /// which does not exist in the compilation, simulating a stale or inconsistent manifest.
    /// </summary>
    private sealed class UnresolvableExternalOptionsProvider : IExternalHierarchicalOptionsProvider
    {
        public IEnumerable<string> GetOptionTypes() => ["NonExistent.StaleOptionsType"];

        public bool TryGetOptions( IDeclaration declaration, string optionsType, [NotNullWhen( true )] out IHierarchicalOptions? options )
        {
            options = null;

            return false;
        }
    }

    /// <summary>
    /// A minimal <see cref="IHierarchicalOptions"/> implementation used to test that
    /// <see cref="HierarchicalOptionsManager.GetOptions"/> handles unregistered option types gracefully.
    /// </summary>
    private sealed class UnresolvableOptions : IHierarchicalOptions
    {
        public object ApplyChanges( object changes, in ApplyChangesContext context ) => this;

        public IHierarchicalOptions? GetDefaultOptions( OptionsInitializationContext context ) => null;
    }
}