// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;
using System.Linq;

// ReSharper disable UnusedType.Local
namespace Metalama.Patterns.Immutability.AspectTests.AttributeAndWarnings.ClassificationTests;

[Immutable( ImmutabilityKind.Deep )]
internal class ClassMarkedDeeplyImmutable;

// ReSharper disable once RedundantArgumentDefaultValue
[Immutable( ImmutabilityKind.Shallow )]
internal class ClassMarkedShallowlyImmutable;

[Immutable( ImmutabilityKind.None )]
internal class ClassMarkedNotImmutable;

internal class ClassNotMarked;

internal readonly struct ReadOnlyStruct;

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