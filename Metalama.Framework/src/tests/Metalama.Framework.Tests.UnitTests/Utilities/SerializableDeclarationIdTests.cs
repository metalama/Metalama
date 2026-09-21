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
using System;
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
    public void KeywordParameterAndReturnParameterDistinctIds()
    {
        const string code = @"
class C
{
    string M(string @return) => @return;
}
";

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( code );
        var type = compilation.Types.Single();
        var method = type.Methods.OfName( "M" ).Single();
        var returnParameter = method.ReturnParameter;
        var regularParameter = method.Parameters.Single();

        // Verify the parameter name is "return" (without @, as Roslyn strips it).
        Assert.Equal( "return", regularParameter.Name );

        // Get IDs for both parameters.
        var returnParamId = returnParameter.ToSerializableId();
        var regularParamId = regularParameter.ToSerializableId();

        this.TestOutput.WriteLine( $"Return parameter ID: {returnParamId.Id}" );
        this.TestOutput.WriteLine( $"Regular parameter ID: {regularParamId.Id}" );

        // The IDs must be different.
        Assert.NotEqual( returnParamId.Id, regularParamId.Id );

        // Verify the return parameter ID contains ";Return".
        Assert.Contains( ";Return", returnParamId.Id, StringComparison.Ordinal );

        // Verify the regular parameter ID contains ";Parameter=0".
        Assert.Contains( ";Parameter=0", regularParamId.Id, StringComparison.Ordinal );

        // Roundtrip both parameters.
        Roundtrip( returnParameter, compilation, this.TestOutput );
        Roundtrip( regularParameter, compilation, this.TestOutput );
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

    /// <summary>
    /// Verifies that every declaration of a file-local type, including a generic one and a nested one, has an
    /// identifier that resolves back to it.
    /// </summary>
    [Fact]
    public void FileLocalTypeRoundtrip()
    {
        const string code = @"
namespace TestNamespace;

file class FileLocalType<T>
{
    public void M<T2>(int p) {}
    public int _field;
    public int Property { get; set; }
    public event System.EventHandler Event;

    public class Nested {}
}
";

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( code );

        foreach ( var declaration in compilation.GetContainedDeclarations() )
        {
            Roundtrip( declaration, compilation, this.TestOutput );
        }
    }

    /// <summary>
    /// Verifies that two file-local types that share a namespace and a name, which is the case the discriminator
    /// exists for, have different identifiers and that each one resolves to the type it was built from.
    /// </summary>
    [Fact]
    public void FileLocalTypesInTwoFilesHaveDistinctIds()
    {
        const string code = @"
namespace TestNamespace;

file class FileLocalType
{
    public void M(int p) {}
}
";

        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilation(
            new Dictionary<string, string> { { "a.cs", code }, { "b.cs", code } } );

        var typeInA = GetFileLocalType( compilation, "a.cs" );
        var typeInB = GetFileLocalType( compilation, "b.cs" );

        Assert.NotSame( typeInA, typeInB );

        var idOfA = typeInA.ToSerializableId();
        var idOfB = typeInB.ToSerializableId();

        this.TestOutput.WriteLine( idOfA.Id );
        this.TestOutput.WriteLine( idOfB.Id );

        Assert.NotEqual( idOfA, idOfB );

        // The discriminator is the metadata name that the compiler gives the file-local type, which begins with the
        // name of the declaring file and continues with the checksum of its path.
        Assert.Contains( ";File=<a>F", idOfA.Id, StringComparison.Ordinal );
        Assert.Contains( ";File=<b>F", idOfB.Id, StringComparison.Ordinal );
        Assert.EndsWith( "__FileLocalType", idOfA.Id, StringComparison.Ordinal );

        Assert.Same( typeInA, idOfA.Resolve( compilation ) );
        Assert.Same( typeInB, idOfB.Resolve( compilation ) );

        // A member of one of them must resolve to that same file, and not to the member of identical signature
        // declared in the other file.
        var methodOfA = typeInA.Methods.OfName( "M" ).Single();
        var methodOfB = typeInB.Methods.OfName( "M" ).Single();

        Assert.NotEqual( methodOfA.ToSerializableId(), methodOfB.ToSerializableId() );
        Assert.Same( methodOfA, methodOfA.ToSerializableId().Resolve( compilation ) );
        Assert.Same( methodOfB, methodOfB.ToSerializableId().Resolve( compilation ) );

        // The same must hold on the symbol path.
        Roundtrip( compilation, typeInA.GetSymbol().AssertSymbolNotNull(), false );
        Roundtrip( compilation, typeInB.GetSymbol().AssertSymbolNotNull(), false );
        Roundtrip( compilation, methodOfA.GetSymbol().AssertSymbolNotNull() );
        Roundtrip( compilation, methodOfB.GetSymbol().AssertSymbolNotNull() );
    }

    /// <summary>
    /// Verifies that an ordinary type and a file-local type that share a namespace and a name are told apart, in both
    /// directions.
    /// </summary>
    [Fact]
    public void FileLocalTypeIsDistinguishedFromOrdinaryType()
    {
        const string ordinaryCode = @"
namespace TestNamespace;

class SameName
{
}
";

        const string fileLocalCode = @"
namespace TestNamespace;

file class SameName
{
}
";

        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilation(
            new Dictionary<string, string> { { "a.cs", ordinaryCode }, { "b.cs", fileLocalCode } } );

        var ordinaryType = compilation.GetContainedDeclarations()
            .OfType<INamedType>()
            .Single( t => t.Name == "SameName" && !IsFileLocal( t ) );

        var fileLocalType = GetFileLocalType( compilation, "b.cs", "SameName" );

        var ordinaryId = ordinaryType.ToSerializableId();
        var fileLocalId = fileLocalType.ToSerializableId();

        Assert.NotEqual( ordinaryId, fileLocalId );

        Assert.Same( ordinaryType, ordinaryId.Resolve( compilation ) );
        Assert.Same( fileLocalType, fileLocalId.Resolve( compilation ) );
    }

    /// <summary>
    /// Verifies that an identifier that carries no discriminator does not resolve to a file-local type, which is the
    /// rule that keeps an identifier written for an ordinary declaration from reaching a file-local one of the same
    /// name.
    /// </summary>
    [Fact]
    public void IdWithoutDiscriminatorDoesNotResolveToFileLocalType()
    {
        const string code = @"
namespace TestNamespace;

file class FileLocalType
{
}
";

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( code );

        // This is the identifier that the documentation comment machinery produces for the file-local type, without
        // the discriminator that Metalama adds.
        var idWithoutDiscriminator = new SerializableDeclarationId( "T:TestNamespace.FileLocalType" );

        Assert.Null( idWithoutDiscriminator.ResolveToSymbolOrNull( compilation.GetCompilationContext() ) );
        Assert.Null( idWithoutDiscriminator.ResolveToDeclaration( compilation ) );
    }

    /// <summary>
    /// Verifies that a reference which cannot be represented by an identifier at all, such as a reference to an
    /// attribute, is reported by a <c>false</c> result rather than by an exception. Reporting it by an exception is
    /// what the non-throwing form exists to avoid.
    /// </summary>
    [Fact]
    public void AttributeReferenceHasNoSerializableId()
    {
        const string code = @"
class C
{
  [System.Obsolete]
  void M() {}
}
";

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( code );

        var attributeReference = compilation.Types.Single().Methods.Single().Attributes.Single().ToRef();

        Assert.False( attributeReference.TryGetSerializableId( out _ ) );
        Assert.Throws<NotSupportedException>( () => attributeReference.ToSerializableId() );
    }

    private static bool IsFileLocal( INamedType type ) => GetTypeSymbol( type ).IsFileLocal;

    private static INamedTypeSymbol GetTypeSymbol( INamedType type ) => type.GetSymbol().AssertSymbolNotNull();

    private static INamedType GetFileLocalType( ICompilation compilation, string filePath, string typeName = "FileLocalType" )
        => compilation.GetContainedDeclarations()
            .OfType<INamedType>()
            .Single(
                t => t.Name == typeName
                     && IsFileLocal( t )
                     && GetTypeSymbol( t ).DeclaringSyntaxReferences[0].SyntaxTree.FilePath == filePath );
}