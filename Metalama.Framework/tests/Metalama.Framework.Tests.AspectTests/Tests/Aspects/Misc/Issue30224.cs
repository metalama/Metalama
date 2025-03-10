// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Text.Json;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Issue30224;

public class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var stringBuilder = BuildInterpolatedString();

        Console.WriteLine( stringBuilder.ToValue() );

        return meta.Proceed();
    }

    [CompileTime]
    protected InterpolatedStringBuilder BuildInterpolatedString()
    {
        var stringBuilder = new InterpolatedStringBuilder();

        stringBuilder.AddText( meta.Target.Method.Name );

        stringBuilder.AddText( "(" );

        var i = 0;

        foreach (var param in meta.Target.Parameters)
        {
            var comma = i > 0 ? ", " : "";

            if (param.RefKind == RefKind.Out)
            {
                stringBuilder.AddText( $"{comma}{param.Name} = " );
            }
            else
            {
                stringBuilder.AddText( param.Name );
                stringBuilder.AddText( " : " );
                var json = JsonSerializer.Serialize( param.Value );
                stringBuilder.AddText( json );
            }

            i++;
        }

        stringBuilder.AddText( ")" );

        return stringBuilder;
    }
}