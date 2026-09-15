// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(15.0)
// @IncludePolyfill(IUnion,UnionAttribute)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;
using System.Linq;

// Covers finding UT-14b of issue #1946: the classification of a union declaration.
//
// A union declaration is shallowly immutable whether or not it carries the readonly modifier, because its only
// instance field is the read-only backing field of the synthesized get-only Value property. It is deeply immutable
// when every case type is. A class that carries the union attribute keeps the classification of an ordinary class,
// because its state is unconstrained.

// ReSharper disable UnusedType.Local
// ReSharper disable UnusedMember.Local
namespace Metalama.Patterns.Immutability.AspectTests.UnionClassificationTests;

[Immutable( ImmutabilityKind.Deep )]
internal class DeeplyImmutableCase;

internal class MutableCase
{
    public int Value { get; set; }
}

internal union DeepUnion( int, string );

internal union DeepUnionOfMarkedCases( DeeplyImmutableCase, long );

internal union ShallowUnion( int, MutableCase );

internal readonly union ReadOnlyShallowUnion( MutableCase, string );

internal union NestedUnion( DeepUnion, int );

/// <summary>
/// A union of the attribute form, whose state the language does not restrict. It must keep the classification of an
/// ordinary class, which is <see cref="ImmutabilityKind.None"/> here because of the settable property.
/// </summary>
[System.Runtime.CompilerServices.Union]
internal class AttributeMarkedUnion
{
    public AttributeMarkedUnion( int value )
    {
        this.Value = value;
    }

    public object? Value { get; }

    public int Mutable { get; set; }
}

// <target>
public class C
{
    private class Fabric : TypeFabric
    {
        [Introduce]
        public static void PrintImmutability()
        {
            foreach ( var type in meta.Target.Compilation.Types.OrderBy( t => t.Name ) )
            {
                meta.InsertComment( $"{type}: {type.GetImmutabilityKind()}" );
            }
        }
    }
}
