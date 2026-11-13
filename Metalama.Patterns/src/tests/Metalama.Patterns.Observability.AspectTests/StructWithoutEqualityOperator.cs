// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Patterns.Observability.AspectTests.StructWithoutEqualityOperator;

public struct StructWithoutOperator
{
    public int X { get; set; }
}

public struct StructWithOperator
{
    public int X { get; set; }

    public static bool operator ==( StructWithOperator a, StructWithOperator b ) => a.X == b.X;

    public static bool operator !=( StructWithOperator a, StructWithOperator b ) => a.X != b.X;

    public override bool Equals( object? obj ) => obj is StructWithOperator other && other.X == this.X;

    public override int GetHashCode() => this.X;
}

public record struct RecordStruct( int X );

public enum MyEnum
{
    A,
    B
}

// <target>
[Observable]
public class C
{
    public StructWithoutOperator WithoutOperator { get; set; }

    public StructWithoutOperator? NullableWithoutOperator { get; set; }

    public StructWithOperator WithOperator { get; set; }

    public StructWithOperator? NullableWithOperator { get; set; }

    public RecordStruct Record { get; set; }

    public MyEnum Enum { get; set; }

    public int Int { get; set; }

    public int? NullableInt { get; set; }

    public DateTime DateTime { get; set; }
}
