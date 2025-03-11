// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Issue31183;

public class MyAspect : TypeAspect
{
    [Template]
    public void Template() { }

    [CompileTime]
    private InterpolatedStringBuilder CreateString()
    {
        var stringBuilder = new InterpolatedStringBuilder();

        // The next line caused the bug.
        stringBuilder.AddText( $"{meta.Target.Type.Name}" );

        return stringBuilder;
    }
}

// <target>
[MyAspect]
internal class C { }