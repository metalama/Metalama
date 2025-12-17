// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.PragmaWarningAroundPropertySignature;

/*
 * Tests that pragma warning directives around property accessor bodies are preserved correctly
 * when the property is overridden by an aspect. Issue #838.
 */

public class OverrideAttribute : OverrideFieldOrPropertyAspect
{
    public override dynamic? OverrideProperty
    {
        get
        {
            Console.WriteLine( "Override getter" );

            return meta.Proceed();
        }

        set
        {
            Console.WriteLine( "Override setter" );
            meta.Proceed();
        }
    }
}

// <target>
internal class TargetClass
{
    private int _value;

    // Pragma warning after getter keyword, before body
    [Override]
    public int PropertyWithPragmaAfterGetter
    {
        get
#pragma warning disable CA1822
        {
            return _value;
        }
#pragma warning restore CA1822
        set
        {
            _value = value;
        }
    }

    // Pragma warning after setter keyword, before body
    [Override]
    public int PropertyWithPragmaAfterSetter
    {
        get
        {
            return _value;
        }
        set
#pragma warning disable CA1822
        {
            _value = value;
        }
#pragma warning restore CA1822
    }
}
