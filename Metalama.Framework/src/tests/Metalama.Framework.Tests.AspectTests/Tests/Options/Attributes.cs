// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Tests.AspectTests.Tests.Options;
#if TEST_OPTIONS
// @Include(_Common.cs)
#endif

[assembly: MyOptions( "Attribute.Assembly" )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Options.Attributes;

[MyOptions( "Attribute.C" )]
[ShowOptionsAspect]
public class C
{
    [ShowOptionsAspect]
    [MyOptions( "Attribute.M" )]
    public void M( int p ) { }
}

[ShowOptionsAspect]
public class D { }