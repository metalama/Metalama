// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Utilities;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Extensibility;

/// <summary>
/// Tests the warning that <see cref="ExtensionLoader"/> reports when an extension assembly has no reference that satisfies the Roslyn version
/// and the target framework of the current process (#2399).
/// </summary>
/// <remarks>
/// The tests call <see cref="ExtensionLoader"/> directly, because <see cref="MetalamaTestContext"/> replaces the extension loader of the
/// pipeline with one that ignores <see cref="IProjectOptions.ExtensionAssemblies"/>.
/// </remarks>
public sealed class ExtensionAssemblyRoslynVersionTests : UnitTestClass
{
    /// <summary>
    /// The name of the extension assembly. No assembly of this name exists.
    /// </summary>
    private const string _extensionName = "Issue2399.Extension";

    /// <summary>
    /// The identifier of the warning that the tests expect.
    /// </summary>
    private const string _warningId = "LAMA0093";

    /// <summary>
    /// A Roslyn version that is higher than the Roslyn version of the current process.
    /// </summary>
    private static readonly Version _higherRoslynVersion = new( 99, 0, 0 );

    /// <summary>
    /// A Roslyn version that is higher than <see cref="_higherRoslynVersion"/>.
    /// </summary>
    private static readonly Version _evenHigherRoslynVersion = new( 100, 0, 0 );

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionAssemblyRoslynVersionTests"/> class.
    /// </summary>
    public ExtensionAssemblyRoslynVersionTests( ITestOutputHelper testOutput ) : base( testOutput ) { }

    /// <summary>
    /// Verifies that a warning is reported when every reference of an extension assembly targets a Roslyn version that is higher than the
    /// Roslyn version of the current process.
    /// </summary>
    [Fact]
    public void ExtensionDeclaredForHigherRoslynVersion_ReportsWarning()
    {
        using var testContext = this.CreateTestContext();

        var diagnostics = GetExtensionTypes(
            testContext,
            CreateReference( testContext, _higherRoslynVersion, null ),
            CreateReference( testContext, _evenHigherRoslynVersion, TargetedAssemblyReference.CurrentTargetFramework ) );

        var warning = Assert.Single( diagnostics, d => d.Id == _warningId );

        Assert.Equal( DiagnosticSeverity.Warning, warning.Severity );

        var message = warning.GetMessage();

        // The message names the extension, the targets that it declares, and the Roslyn version and the target framework of the current process.
        Assert.Contains( _extensionName, message, StringComparison.Ordinal );
        Assert.Contains( "Roslyn " + _higherRoslynVersion, message, StringComparison.Ordinal );
        Assert.Contains( "Roslyn " + _evenHigherRoslynVersion, message, StringComparison.Ordinal );
        Assert.Contains( "Roslyn " + RoslynApiVersion.Current.ToVersion(), message, StringComparison.Ordinal );
        Assert.Contains( $"'{TargetedAssemblyReference.CurrentTargetFramework}'", message, StringComparison.Ordinal );

        // The loader does not try to load any of the references, so no other diagnostic is reported.
        Assert.Single( diagnostics );
    }

    /// <summary>
    /// Verifies that a warning is reported when every reference of an extension assembly targets another target framework.
    /// </summary>
    [Fact]
    public void ExtensionDeclaredForAnotherTargetFramework_ReportsWarning()
    {
        using var testContext = this.CreateTestContext();

        var diagnostics = GetExtensionTypes( testContext, CreateReference( testContext, RoslynApiVersion.Current.ToVersion(), "net1.0" ) );

        var warning = Assert.Single( diagnostics, d => d.Id == _warningId );

        Assert.Contains( "'net1.0'", warning.GetMessage(), StringComparison.Ordinal );
    }

    /// <summary>
    /// Verifies that the reference for the Roslyn version of the current process is selected when the extension also has a reference for an
    /// older Roslyn version.
    /// </summary>
    [Fact]
    public void ExtensionDeclaredForCurrentRoslynVersion_DoesNotReportWarning()
    {
        using var testContext = this.CreateTestContext();

        var diagnostics = GetExtensionTypes(
            testContext,
            CreateReference( testContext, new Version( 1, 0, 0 ), null ),
            CreateReference( testContext, RoslynApiVersion.Current.ToVersion(), null ) );

        Assert.DoesNotContain( diagnostics, d => d.Id == _warningId );

        // The loader selects the reference for the current Roslyn version and tries to load it. The file does not exist, so loading fails.
        var error = Assert.Single( diagnostics );
        Assert.Equal( GeneralDiagnosticDescriptors.CannotLoadExtensionAssembly.Id, error.Id );
        Assert.Contains( RoslynApiVersion.Current.ToVersion().ToString(), error.GetMessage(), StringComparison.Ordinal );
    }

    /// <summary>
    /// Verifies that no warning is reported when the assembly names of an extension end with the Roslyn version, and one of them targets the
    /// current Roslyn version. Metalama Premium names its engine assemblies this way, for instance
    /// <c>Metalama.Extensions.Validation.Engine.5.0.0</c> and <c>Metalama.Extensions.Validation.Engine.5.12.0</c>.
    /// </summary>
    [Fact]
    public void VersionedAssemblyNamesWithCurrentRoslynVersion_DoesNotReportWarning()
    {
        using var testContext = this.CreateTestContext();

        var currentTargetFramework = TargetedAssemblyReference.CurrentTargetFramework;

        var diagnostics = GetExtensionTypes(
            testContext,
            CreateVersionedReference( testContext, new Version( 1, 0, 0 ), "net472" ),
            CreateVersionedReference( testContext, new Version( 1, 0, 0 ), "net10.0" ),
            CreateVersionedReference( testContext, RoslynApiVersion.Current.ToVersion(), currentTargetFramework ) );

        Assert.DoesNotContain( diagnostics, d => d.Id == _warningId );

        // The loader selects the reference for the current Roslyn version and tries to load it. The file does not exist, so loading fails.
        var error = Assert.Single( diagnostics );
        Assert.Equal( GeneralDiagnosticDescriptors.CannotLoadExtensionAssembly.Id, error.Id );
    }

    /// <summary>
    /// Verifies that a single warning, which names the extension without the Roslyn version, is reported when the assembly names of an extension
    /// end with the Roslyn version and all of them target a Roslyn version that is higher than the Roslyn version of the current process.
    /// </summary>
    [Fact]
    public void VersionedAssemblyNamesWithHigherRoslynVersion_ReportsSingleWarning()
    {
        using var testContext = this.CreateTestContext();

        var currentTargetFramework = TargetedAssemblyReference.CurrentTargetFramework;

        var diagnostics = GetExtensionTypes(
            testContext,
            CreateVersionedReference( testContext, _higherRoslynVersion, currentTargetFramework ),
            CreateVersionedReference( testContext, _evenHigherRoslynVersion, currentTargetFramework ) );

        var warning = Assert.Single( diagnostics );
        Assert.Equal( _warningId, warning.Id );

        var message = warning.GetMessage();

        Assert.Contains( $"'{_extensionName}'", message, StringComparison.Ordinal );
        Assert.Contains( "Roslyn " + _higherRoslynVersion, message, StringComparison.Ordinal );
        Assert.Contains( "Roslyn " + _evenHigherRoslynVersion, message, StringComparison.Ordinal );
    }

    /// <summary>
    /// Verifies that, when an extension has no reference for the Roslyn version of the current process, the reference for the highest older
    /// Roslyn version is selected, and the references for a higher Roslyn version are ignored.
    /// </summary>
    [Fact]
    public void VersionedAssemblyNamesWithOlderRoslynVersions_SelectsHighestOlderVersion()
    {
        using var testContext = this.CreateTestContext();

        var currentTargetFramework = TargetedAssemblyReference.CurrentTargetFramework;
        var olderRoslynVersion = new Version( 1, 0, 0 );
        var highestOlderRoslynVersion = new Version( 2, 0, 0 );

        var diagnostics = GetExtensionTypes(
            testContext,
            CreateVersionedReference( testContext, olderRoslynVersion, currentTargetFramework ),
            CreateVersionedReference( testContext, highestOlderRoslynVersion, currentTargetFramework ),
            CreateVersionedReference( testContext, _higherRoslynVersion, currentTargetFramework ) );

        Assert.DoesNotContain( diagnostics, d => d.Id == _warningId );

        // The loader tries to load only the reference for the highest older Roslyn version. The file does not exist, so loading fails.
        var error = Assert.Single( diagnostics );
        Assert.Equal( GeneralDiagnosticDescriptors.CannotLoadExtensionAssembly.Id, error.Id );
        Assert.Contains( $"{_extensionName}.{highestOlderRoslynVersion}.dll", error.GetMessage(), StringComparison.Ordinal );
    }

    /// <summary>
    /// Verifies that <see cref="TargetedAssemblyReference.SelectForCurrentProcess"/> keeps the references that do not declare a Roslyn version,
    /// selects the highest Roslyn version separately for each extension, and preserves the order of the references.
    /// </summary>
    [Fact]
    public void SelectForCurrentProcess_SelectsHighestVersionPerExtension()
    {
        var currentTargetFramework = TargetedAssemblyReference.CurrentTargetFramework;

        var unversioned = new TargetedAssemblyReference( "Unversioned", "Unversioned.dll", null, null );
        var firstOld = new TargetedAssemblyReference( "First.1.0.0", "First.1.0.0.dll", currentTargetFramework, new Version( 1, 0, 0 ) );
        var firstNew = new TargetedAssemblyReference( "First.2.0.0", "First.2.0.0.dll", currentTargetFramework, new Version( 2, 0, 0 ) );
        var secondOld = new TargetedAssemblyReference( "Second", "1.0.0/Second.dll", currentTargetFramework, new Version( 1, 0, 0 ) );
        var secondOtherFramework = new TargetedAssemblyReference( "Second", "3.0.0/Second.dll", "net1.0", new Version( 3, 0, 0 ) );
        var secondHigher = new TargetedAssemblyReference( "Second", "99.0.0/Second.dll", currentTargetFramework, _higherRoslynVersion );

        var selected = TargetedAssemblyReference.SelectForCurrentProcess(
            new[] { firstOld, unversioned, secondOld, firstNew, secondOtherFramework, secondHigher } );

        Assert.Equal( new[] { unversioned, secondOld, firstNew }, selected );
    }

    /// <summary>
    /// Creates a reference to <see cref="_extensionName"/> in a directory that is specific to the Roslyn version and the target framework.
    /// </summary>
    private static TargetedAssemblyReference CreateReference( MetalamaTestContext testContext, Version roslynVersion, string? targetFramework )
        => new(
            _extensionName,
            Path.Combine( testContext.BaseDirectory, "extensions", roslynVersion.ToString(), targetFramework ?? "any", _extensionName + ".dll" ),
            targetFramework,
            roslynVersion );

    /// <summary>
    /// Creates a reference to an assembly whose name is <see cref="_extensionName"/> followed by a dot and the Roslyn version.
    /// </summary>
    private static TargetedAssemblyReference CreateVersionedReference( MetalamaTestContext testContext, Version roslynVersion, string targetFramework )
    {
        var name = _extensionName + "." + roslynVersion;

        return new TargetedAssemblyReference(
            name,
            Path.Combine( testContext.BaseDirectory, "extensions", targetFramework, name + ".dll" ),
            targetFramework,
            roslynVersion );
    }

    /// <summary>
    /// Discovers the extension types of the given references with <see cref="ExtensionLoader"/>, and returns the reported diagnostics.
    /// </summary>
    private static ImmutableArray<Diagnostic> GetExtensionTypes( MetalamaTestContext testContext, params TargetedAssemblyReference[] references )
    {
        var loader = new ExtensionLoader( testContext.ServiceProvider.Global );
        var projectOptions = new ExtensionProjectOptions( testContext.ProjectOptions, references.ToImmutableArray() );
        var diagnostics = new DiagnosticBag();

        var extensionTypes = loader.GetExtensionTypes( projectOptions, testContext.Domain, ExtensionKinds.Default | ExtensionKinds.ServiceFactory, diagnostics )
            .ToList();

        Assert.Empty( extensionTypes );

        return diagnostics.ToImmutableArray();
    }

    /// <summary>
    /// Project options that declare the given extension assemblies.
    /// </summary>
    private sealed class ExtensionProjectOptions : ProjectOptionsWrapper
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExtensionProjectOptions"/> class.
        /// </summary>
        public ExtensionProjectOptions( IProjectOptions wrapped, ImmutableArray<TargetedAssemblyReference> extensionAssemblies ) : base( wrapped )
        {
            this.ExtensionAssemblies = extensionAssemblies;
        }

        /// <inheritdoc />
        public override ImmutableArray<TargetedAssemblyReference> ExtensionAssemblies { get; }
    }
}
