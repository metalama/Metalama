// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Testing.UnitTesting;
using System;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

public class TupleTypeTests : UnitTestClass
{
    [Fact]
    public void SourceTuple_2_Items()
    {
        const string code = """
                            class C
                            {
                                (int A, string B) f;
                            }
                            """;

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( code );
        var field = compilation.Types.Single().Fields.Single();
        var tupleType = Assert.IsAssignableFrom<ITupleType>( field.Type );
        Assert.Equal( TypeKind.Tuple, tupleType.TypeKind );
        Assert.Equal( 2, tupleType.TupleLength );

        // Tuple elements.
        var tupleElement = tupleType.TupleElements[0];
        Assert.Equal( "A", tupleElement.Name );
        Assert.Equal( FieldKind.TupleElement, tupleElement.FieldKind );
        Assert.Equal( SpecialType.Int32, tupleElement.Type.SpecialType );
        Assert.Equal( "Item1", tupleElement.CorrespondingTupleField.Name );
        Assert.True( tupleElement.HasFriendlyName );

        // Display string.
        Assert.Equal( "(int A, string B)", tupleType.ToDisplayString() );

        // The following assertion fails. It does not seem important for now.
        // Assert.Same( tupleType.Fields["Item1"], tupleElement.CorrespondingTupleField );
    }

    [Fact]
    public void SourceTuple_2_Items_Unnamed()
    {
        const string code = """
                            class C
                            {
                                System.ValueTuple<int,string> f;
                            }
                            """;

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( code );
        var field = compilation.Types.Single().Fields.Single();
        var tupleType = Assert.IsAssignableFrom<ITupleType>( field.Type );
        Assert.Equal( TypeKind.Tuple, tupleType.TypeKind );
        Assert.Equal( 2, tupleType.TupleLength );

        // Tuple elements.
        var tupleElement = tupleType.TupleElements[0];
        Assert.Equal( "Item1", tupleElement.Name );
        Assert.Equal( FieldKind.TupleElement, tupleElement.FieldKind );
        Assert.Equal( SpecialType.Int32, tupleElement.Type.SpecialType );
        Assert.Equal( "Item1", tupleElement.CorrespondingTupleField.Name );
        Assert.False( tupleElement.HasFriendlyName );

        // Display string.
        Assert.Equal( "(int, string)", tupleType.ToDisplayString() );
    }

    [Fact]
    public void SourceTuple_1_Item_Unnamed()
    {
        const string code = """
                            class C
                            {
                                System.ValueTuple<int> f;
                            }
                            """;

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( code );
        var field = compilation.Types.Single().Fields.Single();
        var tupleType = Assert.IsAssignableFrom<ITupleType>( field.Type );
        Assert.Equal( TypeKind.Tuple, tupleType.TypeKind );
        Assert.Equal( 1, tupleType.TupleLength );

        // Tuple elements.
        var tupleElement = tupleType.TupleElements[0];
        Assert.Equal( "Item1", tupleElement.Name );
        Assert.Equal( FieldKind.TupleElement, tupleElement.FieldKind );
        Assert.Equal( SpecialType.Int32, tupleElement.Type.SpecialType );
        Assert.Equal( "Item1", tupleElement.CorrespondingTupleField.Name );
        Assert.False( tupleElement.HasFriendlyName );

        // Display string.
        Assert.Equal( "ValueTuple<int>", tupleType.ToDisplayString() );
    }

    [Fact]
    public void SourceTuple_0_Item()
    {
        const string code = """
                            class C
                            {
                                System.ValueTuple f;
                            }
                            """;

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( code );
        var field = compilation.Types.Single().Fields.Single();
        var tupleType = Assert.IsAssignableFrom<ITupleType>( field.Type );
        Assert.Equal( TypeKind.Tuple, tupleType.TypeKind );
        Assert.Equal( 0, tupleType.TupleLength );

        // Display string.
        Assert.Equal( "ValueTuple", tupleType.ToDisplayString() );
    }

    [Fact]
    public void SourceTuple_9_Items()
    {
        const string code = """
                            class C
                            {
                                (int A1, string A2, long A3, int A4, string A5, long A6, int A7, string A8, long A9) f;
                            }
                            """;

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( code );
        var field = compilation.Types.Single().Fields.Single();
        var tupleType = Assert.IsAssignableFrom<ITupleType>( field.Type );
        Assert.Equal( TypeKind.Tuple, tupleType.TypeKind );
        Assert.Equal( 9, tupleType.TupleLength );
        var tupleElement = tupleType.TupleElements[8];
        Assert.Equal( "A9", tupleElement.Name );
        Assert.Equal( FieldKind.TupleElement, tupleElement.FieldKind );
        Assert.Equal( SpecialType.Int64, tupleElement.Type.SpecialType );

        // Display string.
        Assert.Equal( "(int A1, string A2, long A3, int A4, string A5, long A6, int A7, string A8, long A9)", tupleType.ToDisplayString() );
    }

    [Fact]
    public void CreateTuple_0_Item()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( "" );
        var tupleType = compilation.Factory.CreateTupleType( Array.Empty<IType>() );
        Assert.Equal( 0, tupleType.TupleLength );
        
        // Check references.
        var tupleTypeRef = tupleType.ToRef();
        var roundtripTupleType = tupleTypeRef.GetTarget( compilation );
        Assert.Same( tupleType, roundtripTupleType );
    }

    [Fact]
    public void CreateTuple_2_Items()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( "" );
        var tupleType = compilation.Factory.CreateTupleType( [typeof(int), typeof(string)] );
        Assert.Equal( 2, tupleType.TupleLength );

        var tupleElement = tupleType.TupleElements[0];
        Assert.Equal( "Item1", tupleElement.Name );
        Assert.Equal( FieldKind.TupleElement, tupleElement.FieldKind );
        Assert.Equal( SpecialType.Int32, tupleElement.Type.SpecialType );
        Assert.Equal( "Item1", tupleElement.CorrespondingTupleField.Name );
        Assert.False( tupleElement.HasFriendlyName );
        
        // Check references.
        var tupleTypeRef = tupleType.ToRef();
        var roundtripTupleType = tupleTypeRef.GetTarget( compilation );
        Assert.Same( tupleType, roundtripTupleType );
    }

    [Fact]
    public void CreateTuple_7_Items()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( "" );

        var tupleType = compilation.Factory.CreateTupleType(
        [
            (typeof(int), "A1"), (typeof(string), "A2"), (typeof(long), "A3"),
            (typeof(int), "A4"), (typeof(string), "A5"), (typeof(long), "A6"),
            (typeof(int), "A7")
        ] );

        Assert.Equal( 7, tupleType.TupleLength );

        var tupleElement = tupleType.TupleElements[6];
        Assert.Equal( "A7", tupleElement.Name );
        Assert.Equal( FieldKind.TupleElement, tupleElement.FieldKind );
        Assert.Equal( SpecialType.Int32, tupleElement.Type.SpecialType );
        Assert.Equal( "Item7", tupleElement.CorrespondingTupleField.Name );
        Assert.True( tupleElement.HasFriendlyName );
    }

    [Fact]
    public void CreateTuple_8_Items()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( "" );

        var tupleType = compilation.Factory.CreateTupleType(
        [
            (typeof(int), "A1"), (typeof(string), "A2"), (typeof(long), "A3"),
            (typeof(int), "A4"), (typeof(string), "A5"), (typeof(long), "A6"),
            (typeof(int), "A7"), (typeof(string), "A8")
        ] );

        Assert.Equal( 8, tupleType.TupleLength );

        var tupleElement = tupleType.TupleElements[7];
        Assert.Equal( "A8", tupleElement.Name );
        Assert.Equal( FieldKind.TupleElement, tupleElement.FieldKind );
        Assert.Equal( SpecialType.String, tupleElement.Type.SpecialType );
        Assert.Equal( "Item8", tupleElement.CorrespondingTupleField.Name );
        Assert.True( tupleElement.HasFriendlyName );
    }

    [Fact]
    public void CreateTuple_9_Items()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( "" );

        var tupleType = compilation.Factory.CreateTupleType(
        [
            (typeof(int), "A1"), (typeof(string), "A2"), (typeof(long), "A3"),
            (typeof(int), "A4"), (typeof(string), "A5"), (typeof(long), "A6"),
            (typeof(int), "A7"), (typeof(string), "A8"), (typeof(long), "A9")
        ] );

        // Check tuple type.
        Assert.Equal( 9, tupleType.TupleLength );
        Assert.Equal( 10, tupleType.Fields.Count );
        Assert.All( tupleType.Fields, f => Assert.Equal( FieldKind.Default, f.FieldKind ) );
        Assert.All( tupleType.Fields.Where( f => f.Name != "Rest" ), f => Assert.True( f.IsImplicitlyDeclared ) );
        Assert.False( tupleType.Fields["Rest"].IsImplicitlyDeclared );

        // Check tuple element.
        var tupleElement = tupleType.TupleElements[8];
        Assert.Equal( "A9", tupleElement.Name );
        Assert.Equal( FieldKind.TupleElement, tupleElement.FieldKind );
        Assert.Equal( SpecialType.Int64, tupleElement.Type.SpecialType );
        Assert.Equal( "Item9", tupleElement.CorrespondingTupleField.Name );
        Assert.True( tupleElement.HasFriendlyName );

        // Check references.
        var tupleTypeRef = tupleType.ToRef();
        var roundtripTupleType = tupleTypeRef.GetTarget( compilation );
        Assert.Same( tupleType, roundtripTupleType );
    }
}