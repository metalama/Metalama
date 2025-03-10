// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.MetaReturn;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        CalledTemplate();

        return default;
    }

    [Template]
    private void CalledTemplate()
    {
        if (meta.Target.Method.ReturnType.IsConvertibleTo( SpecialType.Void ))
        {
            meta.Return();
        }
        else
        {
            meta.Return( 42 );
        }
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private void VoidMethod() { }

    [Aspect]
    private int IntMethod()
    {
        return -1;
    }
}