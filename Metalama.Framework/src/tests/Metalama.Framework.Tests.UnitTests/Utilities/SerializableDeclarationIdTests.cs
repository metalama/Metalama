// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.Source.Pseudo;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
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
            Roundtrip( compilation, symbol, symbol is not ITypeSymbol );
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
            Assert.Equal( symbol, symbolRoundtrip, SymbolEqualityComparer.IncludeNullability );
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

        Roundtrip( compilation, field.Type.GetSymbol().AssertSymbolNotNull(), false );
    }

    [Fact]
    public void PrimaryConstructorRoundtrip()
    {
        const string code = @"
#pragma warning disable CS9113
class C(int i)
{
  public C(string s) : this(0) {}
}
";

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( code );
        var type = compilation.Types.Single();
        var primaryConstructor = type.PrimaryConstructor;

        if ( primaryConstructor == null )
        {
            // Before Roslyn 4.8, non-record primary constructors are not supported.
            return;
        }

        // Test standard roundtrip (RefTargetKind.Default) for the primary constructor.
        Roundtrip( primaryConstructor, compilation, this.TestOutput );

        // Test roundtrip with RefTargetKind.PrimaryConstructor target kind on the type symbol.
        // This is the path used when [method:] attribute target is applied to a type with a primary constructor.
        var typeSymbol = type.GetSymbol().AssertSymbolNotNull();
        var primaryConstructorSymbol = typeSymbol.InstanceConstructors.First( c => c.IsPrimaryConstructor() );

        var idWithTargetKind = typeSymbol.GetSerializableId( RefTargetKind.PrimaryConstructor );
        this.TestOutput.WriteLine( $"PrimaryConstructor target kind ID: {idWithTargetKind.Id}" );

        // Verify symbol roundtrip via ResolveToSymbolOrNull.
        var resolvedSymbol = idWithTargetKind.ResolveToSymbolOrNull( compilation.GetCompilationContext() );
        Assert.Same( primaryConstructorSymbol, resolvedSymbol );

        // Verify declaration roundtrip via ResolveToDeclaration.
        var resolvedDeclaration = idWithTargetKind.ResolveToDeclaration( compilation.GetCompilationModel() );
        Assert.Same( primaryConstructor, resolvedDeclaration );
    }

    [Fact]
    public void DelegateReturnParameterRoundtrip()
    {
        const string code = @"
namespace TestNamespace;

delegate int D(int x, string y);
";

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( code );
        var delegateType = compilation.GetContainedDeclarations().OfType<INamedType>().Single( t => t.Name == "D" );
        var invokeMethod = delegateType.Methods.OfName( "Invoke" ).Single();
        var returnParameter = invokeMethod.ReturnParameter;

        // Test standard roundtrip for the delegate's return parameter (through the Invoke method).
        Roundtrip( returnParameter, compilation, this.TestOutput );

        // Test roundtrip with RefTargetKind.Return on the delegate type symbol.
        // This is the path used when [return:MyAspect] is applied to a delegate declaration.
        var delegateTypeSymbol = delegateType.GetSymbol().AssertSymbolNotNull();

        var idWithTargetKind = delegateTypeSymbol.GetSerializableId( RefTargetKind.Return );
        this.TestOutput.WriteLine( $"Delegate Return target kind ID: {idWithTargetKind.Id}" );

        // Verify symbol roundtrip via ResolveToSymbolOrNull (two-parameter version).
        // The single-parameter version returns null for return parameters (by design).
        var resolvedSymbol = idWithTargetKind.ResolveToSymbolOrNull( compilation.GetCompilationContext(), out var isReturnParameter );
        Assert.True( isReturnParameter );
        Assert.NotNull( resolvedSymbol );
        Assert.Equal( delegateTypeSymbol, resolvedSymbol, SymbolEqualityComparer.Default );

        // Verify declaration roundtrip via ResolveToDeclaration.
        var resolvedDeclaration = idWithTargetKind.ResolveToDeclaration( compilation.GetCompilationModel() );
        Assert.Same( returnParameter, resolvedDeclaration );
    }

    [Fact]
    public void DelegateParameterRoundtrip()
    {
        const string code = @"
namespace TestNamespace;

delegate int D(int x, string y);
";

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( code );
        var delegateType = compilation.GetContainedDeclarations().OfType<INamedType>().Single( t => t.Name == "D" );
        var invokeMethod = delegateType.Methods.OfName( "Invoke" ).Single();

        // Test standard roundtrip for the delegate's parameters (through the Invoke method).
        foreach ( var parameter in invokeMethod.Parameters )
        {
            Roundtrip( parameter, compilation, this.TestOutput );
        }
    }

    [Fact]
    public void FileLocalTypes_HaveDistinctIds()
    {
        // Two file-local types with the same name and namespace but in different files
        // must produce different SerializableDeclarationIds.
        const string code1 = @"
namespace TestNamespace;

file class FileLocalType
{
    public int X;
}
";

        const string code2 = @"
namespace TestNamespace;

file class FileLocalType
{
    public string Y;
}
";

        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilation(
            new Dictionary<string, string>
            {
                { "File1.cs", code1 },
                { "File2.cs", code2 }
            } );

        var fileLocalTypes = compilation.GetContainedDeclarations()
            .OfType<INamedType>()
            .Where( t => t.Name == "FileLocalType" )
            .ToList();

        Assert.Equal( 2, fileLocalTypes.Count );

        var id1 = fileLocalTypes[0].ToSerializableId();
        var id2 = fileLocalTypes[1].ToSerializableId();

        this.TestOutput.WriteLine( $"File-local type 1 ID: {id1.Id}" );
        this.TestOutput.WriteLine( $"File-local type 2 ID: {id2.Id}" );

        // The two file-local types must have different serializable IDs.
        Assert.NotEqual( id1, id2 );

        // Both must roundtrip correctly.
        Roundtrip( fileLocalTypes[0], compilation, this.TestOutput );
        Roundtrip( fileLocalTypes[1], compilation, this.TestOutput );
    }

    [Fact]
    public void FileLocalTypes_MembersHaveDistinctIds()
    {
        // Members of file-local types with the same name must also have distinct IDs.
        const string code1 = @"
namespace TestNamespace;

file class FileLocalType
{
    public void M() {}
}
";

        const string code2 = @"
namespace TestNamespace;

file class FileLocalType
{
    public void M() {}
}
";

        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilation(
            new Dictionary<string, string>
            {
                { "File1.cs", code1 },
                { "File2.cs", code2 }
            } );

        var fileLocalTypes = compilation.GetContainedDeclarations()
            .OfType<INamedType>()
            .Where( t => t.Name == "FileLocalType" )
            .ToList();

        Assert.Equal( 2, fileLocalTypes.Count );

        var method1 = fileLocalTypes[0].Methods.OfName( "M" ).Single();
        var method2 = fileLocalTypes[1].Methods.OfName( "M" ).Single();

        var methodId1 = method1.ToSerializableId();
        var methodId2 = method2.ToSerializableId();

        this.TestOutput.WriteLine( $"Method 1 ID: {methodId1.Id}" );
        this.TestOutput.WriteLine( $"Method 2 ID: {methodId2.Id}" );

        // Methods on different file-local types must have different IDs.
        Assert.NotEqual( methodId1, methodId2 );

        // Both must roundtrip correctly.
        Roundtrip( method1, compilation, this.TestOutput );
        Roundtrip( method2, compilation, this.TestOutput );
    }

    [Fact]
    public void FileLocalTypes_ParametersHaveDistinctIds()
    {
        // Parameters of methods on file-local types with the same name must have distinct IDs.
        const string code1 = @"
namespace TestNamespace;

file class FileLocalType
{
    public void M(int p) {}
}
";

        const string code2 = @"
namespace TestNamespace;

file class FileLocalType
{
    public void M(int p) {}
}
";

        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilation(
            new Dictionary<string, string>
            {
                { "File1.cs", code1 },
                { "File2.cs", code2 }
            } );

        var fileLocalTypes = compilation.GetContainedDeclarations()
            .OfType<INamedType>()
            .Where( t => t.Name == "FileLocalType" )
            .ToList();

        Assert.Equal( 2, fileLocalTypes.Count );

        var param1 = fileLocalTypes[0].Methods.OfName( "M" ).Single().Parameters.Single();
        var param2 = fileLocalTypes[1].Methods.OfName( "M" ).Single().Parameters.Single();

        var paramId1 = param1.ToSerializableId();
        var paramId2 = param2.ToSerializableId();

        this.TestOutput.WriteLine( $"Parameter 1 ID: {paramId1.Id}" );
        this.TestOutput.WriteLine( $"Parameter 2 ID: {paramId2.Id}" );

        // Parameters on different file-local types must have different IDs.
        Assert.NotEqual( paramId1, paramId2 );

        // Both must roundtrip correctly.
        Roundtrip( param1, compilation, this.TestOutput );
        Roundtrip( param2, compilation, this.TestOutput );
    }

    [Fact]
    public void FileLocalTypes_TypeParametersHaveDistinctIds()
    {
        // Type parameters of methods on file-local types with the same name must have distinct IDs.
        const string code1 = @"
namespace TestNamespace;

file class FileLocalType
{
    public void M<T>() {}
}
";

        const string code2 = @"
namespace TestNamespace;

file class FileLocalType
{
    public void M<T>() {}
}
";

        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilation(
            new Dictionary<string, string>
            {
                { "File1.cs", code1 },
                { "File2.cs", code2 }
            } );

        var fileLocalTypes = compilation.GetContainedDeclarations()
            .OfType<INamedType>()
            .Where( t => t.Name == "FileLocalType" )
            .ToList();

        Assert.Equal( 2, fileLocalTypes.Count );

        var typeParam1 = fileLocalTypes[0].Methods.OfName( "M" ).Single().TypeParameters.Single();
        var typeParam2 = fileLocalTypes[1].Methods.OfName( "M" ).Single().TypeParameters.Single();

        var typeParamId1 = typeParam1.ToSerializableId();
        var typeParamId2 = typeParam2.ToSerializableId();

        this.TestOutput.WriteLine( $"TypeParameter 1 ID: {typeParamId1.Id}" );
        this.TestOutput.WriteLine( $"TypeParameter 2 ID: {typeParamId2.Id}" );

        // Type parameters on different file-local types must have different IDs.
        Assert.NotEqual( typeParamId1, typeParamId2 );

        // Both must roundtrip correctly.
        Roundtrip( typeParam1, compilation, this.TestOutput );
        Roundtrip( typeParam2, compilation, this.TestOutput );
    }
}