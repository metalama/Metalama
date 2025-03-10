// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Fields.PrivateField_CrossAssembly;

public class IntroducePrivateFieldAttribute : OverrideMethodAspect
{
    [Introduce]
    private readonly string _text = "a text";

    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( _text );

        return meta.Proceed();
    }
}

internal class IC { }