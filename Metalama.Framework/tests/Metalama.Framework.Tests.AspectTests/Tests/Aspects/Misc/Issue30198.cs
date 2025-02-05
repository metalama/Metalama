using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Issue30198;

#pragma warning disable CS0219

public class ExcludeLoggingAttribute : Attribute { }

public class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        // 'typeof' in an 'if'
        if (!meta.Target.Method.Attributes.Any( a => a.Type.IsConvertibleTo( typeof(ExcludeLoggingAttribute) ) ))
        {
            Console.WriteLine( "Hello, world." );
        }

        // 'typeof' in a 'foreach'
        foreach (var p in meta.Target.Parameters.Where( p => !p.Attributes.Any( a => a.Type.IsConvertibleTo( typeof(ExcludeLoggingAttribute) ) ) ))
        {
            Console.WriteLine( $"Param {p}" );
        }

        // 'nameof'
        var n = nameof(ExcludeLoggingAttribute);

        return meta.Proceed();
    }
}

// <target>
internal class Target
{
    [Aspect]
    private void M( int x )
    {
        _ = x;
    }
}