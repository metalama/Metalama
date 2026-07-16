// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
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
