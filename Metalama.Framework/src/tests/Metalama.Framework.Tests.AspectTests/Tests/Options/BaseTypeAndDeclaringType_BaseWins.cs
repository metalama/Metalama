// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @Include(_Common.cs)
#endif

namespace Metalama.Framework.Tests.AspectTests.Tests.Options.BaseTypeAndDeclaringType_BaseWins;

[MyOptions( "Attribute.C", true )]
[ShowOptionsAspect]
public class C { }

[MyOptions( "Attribute.P" )]
[ShowOptionsAspect]
public class P
{
    // <target>
    [ShowOptionsAspect]
    public class D : C { }
}