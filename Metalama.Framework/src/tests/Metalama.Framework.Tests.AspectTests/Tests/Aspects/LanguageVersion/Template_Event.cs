// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.LanguageVersion.Template_Event;

public class TheAspect : TypeAspect
{
    [Introduce]
    public event EventHandler Event1
    {
        add
        {
            Console.WriteLine( """add""" );
        }
        remove { }
    }

    [Introduce]
    public event EventHandler Event2
    {
        add { }
        remove
        {
            Console.WriteLine( """remove""" );
        }
    }
}

// <target>
[TheAspect]
internal class Target { }