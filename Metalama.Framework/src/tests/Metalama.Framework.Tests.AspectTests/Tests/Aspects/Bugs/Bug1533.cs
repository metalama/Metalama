// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug1533;

/*
 * Regression test for #1533: NullReferenceException in ProduceCompileTimeCodeRewriter.TransformCompileTimeType
 * when a compile-time type (aspect) contains nested type declarations (class, record, enum, delegate).
 * These nested types fall through to the default case in TransformCompileTimeType's member switch,
 * where Visit() returns null for runtime-only types, causing NRE in SyntaxList.CreateNode().
 */

public sealed class AspectWithNestedTypes : TypeAspect
{
    // Nested class inside the aspect (a compile-time type).
    // This is runtime-only and triggers the bug in the default case.
    public class NestedClass
    {
        public string? Name { get; set; }
    }

    // Nested record class inside the aspect.
    public record NestedRecord( string Value );

    // Nested enum inside the aspect.
    public enum NestedEnum
    {
        None,
        First,
        Second
    }

    // Nested delegate inside the aspect.
    public delegate void NestedDelegate( string message );

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(IntroducedMethod) );
    }

    [Template]
    public void IntroducedMethod()
    {
        Console.WriteLine( "Introduced" );
    }
}

// <target>
[AspectWithNestedTypes]
public class TargetClass
{
}
