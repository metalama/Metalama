// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.LanguageVersion.Template_Property;

public class TheAspect : TypeAspect
{
    [Introduce]
    public string Property1
    {
        get
        {
            return """get""";
        }
    }

    [Introduce]
    public string Property2
    {
        get => "";
        set
        {
            Console.WriteLine( """set""" );
        }
    }
}

// <target>
[TheAspect]
internal class Target { }