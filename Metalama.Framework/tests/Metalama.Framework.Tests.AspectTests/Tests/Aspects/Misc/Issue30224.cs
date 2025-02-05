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