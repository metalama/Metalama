// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS8618, CS8602

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug30448;

internal class TrimAttribute : ContractAspect
{
    public override void Validate( dynamic? value )
    {
        value = value?.Trim();
    }
}

// <target>
internal class Foo
{
    public void Method1( [Trim] string nonNullableString, [Trim] string? nullableString )
    {
        Console.WriteLine( $"nonNullableString='{nonNullableString}', nullableString='{nullableString}'" );
    }

    public string? Property { get; set; }
}