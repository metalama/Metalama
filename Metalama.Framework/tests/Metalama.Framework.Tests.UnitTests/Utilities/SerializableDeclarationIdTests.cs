// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Source.Pseudo;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using MethodKind = Metalama.Framework.Code.MethodKind;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

public sealed class SerializableDeclarationIdTests : UnitTestClass
{
    public SerializableDeclarationIdTests( ITestOutputHelper? logger = null ) : base( logger, false ) { }

    [Fact]
    public void AssemblyAndCompilation()
    {
        using var testContext = this.CreateTestContext();
        var referencedCompilation = testContext.CreateCompilation( "public class A {}" );

        var mainCompilation = testContext.CreateCompilation(
            "class B : A {}",
            additionalReferences: new[] { referencedCompilation.GetRoslynCompilation().ToMetadataReference() } );

        var assemblyReference = mainCompilation.Types.Single().BaseType!.DeclaringAssembly;

        var referencedCompilationId = referencedCompilation.ToSerializableId();
        var assemblyReferenceId = assemblyReference.ToSerializableId();

        Assert.Equal( referencedCompilationId, assemblyReferenceId );
    }

    [Fact]
    public void TestAllDeclarations()
    {
        const string code = @"
namespace Metalama;

delegate void D();

class C<T> 
{
  void M<T2>(int p) {}
  int this[int i] => 0;
  int _field;
  int Property { get; set; }
  event System.EventHandler Event;

  C() {}
  ~C() {}

  static C() {}

  class N<T2> {}
}
";

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( code );

        foreach ( var declaration in compilation.GetContainedDeclarations() )
        {
            Roundtrip( declaration, compilation, this.TestOutput );
        }

        Roundtrip( compilation, compilation, this.TestOutput );
    }

    internal static void Roundtrip( IDeclaration declaration, ICompilation compilation, ITestOutputHelper testOutput )
    {
        if ( declaration is PseudoParameter && declaration.ContainingDeclaration is IMethod { MethodKind: MethodKind.EventRaise } )
        {
            // Not yet implemented.
            return;
        }

        // Test declaration roundtrip from reference
        var roundtripFromReference = declaration.ToRef().GetTarget( compilation );
        Assert.Same( declaration, roundtripFromReference );

        // Test declaration roundtrip from serialization.
        var declarationId = declaration.ToSerializableId();
        testOutput.WriteLine( declarationId.Id );
        var roundtripFromDeclarationId = declarationId.Resolve( compilation );

        if ( declaration is INamespace ns )
        {
            // compilation.GetContainedDeclarations() contains compilation-specific namespaces,
            // but Resolve() returns a merged namespace (which includes types from references),
            // so Assert.Same would fail here.
            Assert.Equal( ns.FullName, ((INamespace) roundtripFromDeclarationId).FullName );
        }
        else
        {
            Assert.Same( declaration, roundtripFromDeclarationId );
        }

        if ( declaration is INamespace { IsGlobalNamespace: true } )
        {
            // Roslyn does not support this, see https://github.com/dotnet/roslyn/issues/66976,
            // so skip testing symbols.
            return;
        }

        // Test symbol roundtrip.
        var symbol = declaration.GetSymbol();

        if ( symbol != null )
        {
            Roundtrip( compilation, symbol );
        }
    }

    private static void Roundtrip( ICompilation compilation, ISymbol symbol, bool requireSameInstance = true )
    {
        var symbolDeclarationId = symbol.GetSerializableId();
        var symbolRoundtrip = symbolDeclarationId.ResolveToSymbolOrNull( compilation.GetCompilationContext() );

        if ( symbol is INamespaceSymbol nss )
        {
            Assert.Equal( nss.GetFullName(), (symbolRoundtrip as INamespaceSymbol)?.GetFullName() );
        }
        else if ( requireSameInstance )
        {
            Assert.Same( symbol, symbolRoundtrip );
        }
        else
        {
            Assert.Equal( symbol, symbolRoundtrip, SymbolEqualityComparer.Default );
        }

        // Also test a Ref roundtrip.
        var symbolRoundtripFromRef = compilation
            .GetRefFactory()
            .FromAnySymbol( symbol )
            .GetSymbol( compilation.GetRoslynCompilation() );

        if ( requireSameInstance )
        {
            Assert.Same( symbol, symbolRoundtripFromRef );
        }
        else
        {
            Assert.Equal( symbol, symbolRoundtripFromRef, SymbolEqualityComparer.Default );
        }
    }

    [Fact]
    public void TestNonNamedTyped()
    {
        const string code = @"
class C 
{
  public int[] F;
}
";

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( code );
        var field = compilation.Types.Single().Fields.Single();

        Roundtrip( compilation, field.Type.GetSymbol(), false );
    }
}