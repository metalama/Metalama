// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug812_KeywordEscaping_MethodParams;

// Issue #812: Tests contracts on parameters with C# keyword names

internal class NotNullAttribute : ContractAspect
{
    public override void Validate( dynamic? value )
    {
        if ( value == null )
        {
            throw new ArgumentNullException();
        }
    }
}

internal class PositiveAttribute : ContractAspect
{
    public override void Validate( dynamic? value )
    {
        if ( value <= 0 )
        {
            throw new ArgumentOutOfRangeException();
        }
    }
}

// <target>
internal class TargetClass
{
    // Method with single keyword parameter name
    public void MethodWithKeywordParam( [NotNull] string @class )
    {
        Console.WriteLine( $"class = {@class}" );
    }

    // Method with multiple keyword parameter names
    public int MethodWithMultipleKeywordParams( [Positive] int @int, [NotNull] string @string, bool @return )
    {
        Console.WriteLine( $"int = {@int}, string = {@string}, return = {@return}" );

        return @int;
    }

    // Constructor with keyword parameter names
    public TargetClass( [NotNull] string @class, [Positive] int @for )
    {
        Console.WriteLine( $"class = {@class}, for = {@for}" );
    }
}
