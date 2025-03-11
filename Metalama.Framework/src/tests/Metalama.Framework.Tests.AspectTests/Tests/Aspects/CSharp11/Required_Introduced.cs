// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(NET7_0_OR_GREATER)
#endif

#if NET7_0_OR_GREATER
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects; 

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.Required_Introduced;

public class TheAspect : TypeAspect
{
    [Introduce( IsRequired = true )]
    public int Field;

    [Introduce( IsRequired = true )]
    public int Property { get; set; }
}

// <target>
[TheAspect]
public class C { }

#endif