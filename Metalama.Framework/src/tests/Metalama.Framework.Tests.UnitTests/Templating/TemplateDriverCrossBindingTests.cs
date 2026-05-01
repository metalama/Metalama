// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.CompileTime.Serialization;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Templating;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Templating;

/// <summary>
/// Tests for the cross-binding bug (issue metalama/Metalama#1611). Two compile-time-projection assemblies
/// for the same logical upstream coexist in one process; the deserialised <see cref="IAspect"/> ends up
/// paired with an <see cref="IAspectClass"/> from the other physical project. <c>MethodInfo.Invoke</c> on
/// the template method then throws <see cref="System.Reflection.TargetException"/>.
/// </summary>
public sealed class TemplateDriverCrossBindingTests : UnitTestClass
{
    private const string _aspectSource = @"
using Metalama.Framework.Aspects;
namespace Cross.Binding.Test
{
    public class TestAspect : IAspect
    {
        public object? Method() => null;
    }
}
";

    /// <summary>
    /// Unit-level guard: the <see cref="TemplateDriver.InvokeTemplate"/> pre-flight detects mismatched declaring
    /// type and target instance type, and throws a rich diagnostic naming both assembly identities and locations.
    /// </summary>
    [Fact]
    public void TemplateDriver_Throws_WhenTargetTypeIsNotAssignableToDeclaringType()
    {
        var assembly1 = CompileToAssembly( "Cross.Binding.Test.A1" );
        var assembly2 = CompileToAssembly( "Cross.Binding.Test.A2" );

        var type1 = assembly1.GetType( "Cross.Binding.Test.TestAspect" )!;
        var type2 = assembly2.GetType( "Cross.Binding.Test.TestAspect" )!;
        Assert.NotSame( type1, type2 );

        var declaringMethod = type1.GetMethod( "Method" )!;
        var crossBoundInstance = Activator.CreateInstance( type2 )!;

        using var testContext = this.CreateTestContext();
        var driver = new TemplateDriver( testContext.ServiceProvider, declaringMethod );

        var ex = Assert.Throws<InvalidOperationException>( () => driver.InvokeTemplate( crossBoundInstance, [] ) );

        Assert.Contains( "two distinct copies of the same logical assembly", ex.Message );
        Assert.Contains( "Declaring assembly:", ex.Message );
        Assert.Contains( "Actual assembly:", ex.Message );
        Assert.Contains( assembly1.GetName().Name!, ex.Message );
        Assert.Contains( assembly2.GetName().Name!, ex.Message );
    }

    /// <summary>
    /// Sanity baseline: when the instance type matches the declaring type, the pre-flight does not false-positive.
    /// </summary>
    [Fact]
    public void TemplateDriver_DoesNotThrow_WhenTargetTypeMatchesDeclaringType()
    {
        var assembly = CompileToAssembly( "Cross.Binding.Test.OK" );
        var type = assembly.GetType( "Cross.Binding.Test.TestAspect" )!;

        var method = type.GetMethod( "Method" )!;
        var instance = Activator.CreateInstance( type )!;

        using var testContext = this.CreateTestContext();
        var driver = new TemplateDriver( testContext.ServiceProvider, method );

        var result = driver.InvokeTemplate( instance, [] );
        Assert.Null( result );
    }

    /// <summary>
    /// Regression test for the architectural fix in issue metalama/Metalama#1611. When two physically distinct
    /// <see cref="CompileTimeProject"/> instances exist for the same logical upstream, the deserialiser must
    /// resolve types through the specific anchor's closure — not through whatever <see cref="CompileTimeProject"/>
    /// is registered in the service provider (which may pick the other physical project depending on closure
    /// dedup policy). The fix adds an upstream-project parameter to <c>CompileTimeSerializer.CreateInstance</c>
    /// and to <c>TransitiveAspectsManifest.Deserialize</c>; the call site at
    /// <c>TransitivePipelineContributorSource</c> looks the upstream project up by <see cref="AssemblyIdentity"/>
    /// from the <see cref="CompileTimeProjectRepository"/>.
    /// </summary>
    /// <remarks>
    /// Without the upstream-project overload (the pre-fix code path), the binder is built from
    /// <c>serviceProvider.GetService&lt;CompileTimeProject&gt;()</c> only — there is no way to anchor it.
    /// This test relies on the new overload existing; it verifies that the anchor wins over the service-provider-
    /// resolved project. With the architectural fix in place at the production call site, the deserialised aspect
    /// is type-aligned with the upstream's <c>IAspectClass.Type</c> by construction.
    /// </remarks>
    [Fact]
    public void CompileTimeSerializer_AnchoredToUpstreamProject_ResolvesAspectTypeFromAnchor()
    {
        using var testContext = this.CreateTestContext();

        // Two distinct CompileTimeProjects with the SAME run-time identity name but distinct compile-time
        // identities (different file paths => different source hashes => distinct compile-time assemblies).
        // This mirrors the production scenario where two physical projects coexist for one logical upstream.
        var (compilationA, projectA, typeFromA) = BuildAspectProject( testContext, "TestUpstream", "/A.cs" );
        var (_, projectB, typeFromB) = BuildAspectProject( testContext, "TestUpstream", "/B.cs" );

        Assert.Equal( "TestUpstream", projectA.RunTimeIdentity.Name );
        Assert.Equal( "TestUpstream", projectB.RunTimeIdentity.Name );
        Assert.NotEqual( projectA.CompileTimeIdentity, projectB.CompileTimeIdentity );
        Assert.NotSame( typeFromA, typeFromB );
        Assert.NotSame( typeFromA.Assembly, typeFromB.Assembly );

        var aspectFromA = (IAspect) Activator.CreateInstance( typeFromA )!;

        // Serialize the aspect directly (IAspect is ICompileTimeSerializable) using projectA as the anchor:
        // the serializer's binder records the type's run-time assembly name via
        // projectA.TryGetProjectByCompileTimeAssemblyName, producing "TestUpstream".
        using var stream = new MemoryStream();

        var serializer = CompileTimeSerializer.CreateInstance(
            testContext.ServiceProvider,
            compilationA.CompilationContext,
            projectA );

        serializer.Serialize( aspectFromA, stream );

        // Deserialize with the anchor swapped to projectB. The architectural fix's contract: the binder uses
        // projectB.TryGetType to resolve "Cross.Binding.Test.TestAspect" + "TestUpstream", which walks projectB's
        // closure and produces typeFromB — NOT typeFromA, even though projectA is the same kind of upstream.
        stream.Position = 0;

        var deserializer = CompileTimeSerializer.CreateInstance(
            testContext.ServiceProvider,
            compilationA.CompilationContext,
            projectB );

        var deserialized = (IAspect) deserializer.Deserialize( stream )!;

        Assert.Same( typeFromB, deserialized.GetType() );
        Assert.NotSame( typeFromA, deserialized.GetType() );
    }

    private static (CompilationModel Compilation, CompileTimeProject Project, Type AspectType) BuildAspectProject(
        TestContext testContext,
        string assemblyName,
        string filePath )
    {
        var compilation = testContext.CreateCompilationModel(
            new Dictionary<string, string> { { filePath, _aspectSource } },
            name: assemblyName );

        var repository = CompileTimeProjectRepository.Create(
                testContext.Domain,
                testContext.ServiceProvider,
                compilation.RoslynCompilation,
                NullDiagnosticAdder.Instance )
            .AssertNotNull();

        var project = repository.RootProject;
        var aspectType = project.GetType( "Cross.Binding.Test.TestAspect" );

        return (compilation, project, aspectType);
    }

    private static Assembly CompileToAssembly( string assemblyName )
    {
        var syntaxTree = CSharpSyntaxTree.ParseText( _aspectSource );

        var references = new List<MetadataReference>();
        var hostAssemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach ( var hostAssembly in hostAssemblies )
        {
            if ( hostAssembly.IsDynamic )
            {
                continue;
            }

            if ( string.IsNullOrEmpty( hostAssembly.Location ) )
            {
                continue;
            }

            var name = hostAssembly.GetName().Name;

            if ( name is "mscorlib" or "System.Private.CoreLib" or "System.Runtime" or "netstandard"
                or "Metalama.Framework" )
            {
                references.Add( MetadataReference.CreateFromFile( hostAssembly.Location ) );
            }
        }

        var compilation = CSharpCompilation.Create(
            assemblyName,
            [syntaxTree],
            references,
            new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary ) );

        using var stream = new MemoryStream();
        var emitResult = compilation.Emit( stream );

        if ( !emitResult.Success )
        {
            throw new InvalidOperationException(
                $"Failed to compile test fixture '{assemblyName}': "
                + string.Join( "; ", emitResult.Diagnostics ) );
        }

        return Assembly.Load( stream.ToArray() );
    }
}
