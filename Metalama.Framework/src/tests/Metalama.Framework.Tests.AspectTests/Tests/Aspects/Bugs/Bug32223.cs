// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32223;

#pragma warning disable CS0168, CS8618, CS0169

public class Base<T> { }

// <target>
public class C : Base<dynamic>
{
    private dynamic _f1;
    private dynamic[] _f2;
    private List<dynamic> _f3;

    private dynamic M( dynamic x )
    {
        dynamic l1;
        dynamic[] l2;
        List<dynamic> l3;

        return x;
    }
}