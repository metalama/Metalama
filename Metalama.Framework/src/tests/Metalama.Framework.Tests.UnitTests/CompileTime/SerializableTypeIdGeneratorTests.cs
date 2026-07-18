// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

/// <summary>
/// Covers <see cref="SerializableTypeIdGenerator"/>, most importantly the invariant that the reflection-<see cref="Type"/>
/// overload produces the <em>same</em> id as the <see cref="IType"/>/<see cref="ITypeSymbol"/> overload for the same
/// logical type. All three keep <see cref="Metalama.Framework.Engine.ReflectionMocks.CompileTimeType"/> instances
/// consistent -- equality and the factory cache key on the id string -- so a divergence silently duplicates cache
/// entries and breaks mock equality.
/// </summary>
public sealed class SerializableTypeIdGeneratorTests : UnitTestClass
{
    public static IEnumerable<object[]> Types =>
    [
        [typeof(int)],
        [typeof(string)],
        [typeof(object)],
        [typeof(DayOfWeek)],                    // enum
        [typeof(Guid)],                         // struct
        [typeof(int[])],
        [typeof(int[,])],                       // multi-dimensional
        [typeof(int[][])],                      // jagged
        [typeof(string[])],
        [typeof(DayOfWeek[])],                  // array of enum
        [typeof(List<int>)],                    // constructed generic
        [typeof(List<string>)],
        [typeof(Dictionary<string, int>)],      // two type arguments
        [typeof(List<int>[])],                  // array of generic
        [typeof(List<List<int>>)],              // nested generic
        [typeof(KeyValuePair<string, int>)],    // value-type generic
        [typeof(int?)],                         // Nullable<int>
        [typeof(Environment.SpecialFolder)],    // nested enum
        [typeof(DateTimeKind)],
        [typeof(List<>)],                       // open generic definition
        [typeof(Dictionary<,>)]                 // open generic definition, arity 2
    ];

    /// <summary>
    /// The reflection-<see cref="Type"/> overload and the <see cref="IType"/> overload must produce byte-identical ids.
    /// </summary>
    [Theory]
    [MemberData( nameof(Types) )]
    public void ReflectionTypeId_EqualsCodeModelTypeId( Type type )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "" );

        var codeModelType = compilation.Factory.GetTypeByReflectionType( type );

        var reflectionId = type.GetSerializableTypeId().Id;
        var codeModelId = codeModelType.GetSerializableTypeId().Id;

        Assert.Equal( codeModelId, reflectionId );
    }

    // Closed types only: an open generic definition (List<>) is not something one resolves to a concrete IType.
    public static IEnumerable<object[]> ClosedTypes =>
    [
        [typeof(int)], [typeof(string)], [typeof(object)], [typeof(DayOfWeek)], [typeof(Guid)],
        [typeof(int[])], [typeof(int[,])], [typeof(int[][])], [typeof(string[])], [typeof(DayOfWeek[])],
        [typeof(List<int>)], [typeof(List<string>)], [typeof(Dictionary<string, int>)], [typeof(List<int>[])],
        [typeof(List<List<int>>)], [typeof(KeyValuePair<string, int>)], [typeof(int?)],
        [typeof(Environment.SpecialFolder)], [typeof(DateTimeKind)]
    ];

    /// <summary>
    /// The id must round-trip: resolving the id the reflection overload produces yields the original type.
    /// </summary>
    [Theory]
    [MemberData( nameof(ClosedTypes) )]
    public void ReflectionTypeId_RoundTrips( Type type )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "" );

        var expected = compilation.Factory.GetTypeByReflectionType( type );
        var id = type.GetSerializableTypeId();

        var resolved = id.Resolve( compilation );

        Assert.Equal( expected.ToDisplayString(), resolved.ToDisplayString() );
    }

    [Fact]
    public void ReflectionTypeId_IsStable()
    {
        // The (weakly) cached path must return the same value on a second call.
        Assert.Equal( typeof(List<int>).GetSerializableTypeId().Id, typeof(List<int>).GetSerializableTypeId().Id );
    }
}
