using System;
using System.Collections.Generic;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateParameters.Syntax;

#pragma warning disable CS8602

internal class MyAspect : ContractAspect
{
    public override void Validate( dynamic? value )
    {
        if (meta.Target.Parameter.Type is INamedType namedType
            && namedType.ImplementedInterfaces.Any( i => i.Definition.IsConvertibleTo( typeof(IEnumerable<>) ) ))
        {
            foreach (var c in value)
            {
                Console.WriteLine( c );
            }
        }
        else if (meta.Target.Parameter.Type.IsConvertibleTo( typeof(bool) ))
        {
            if (value)
            {
                Console.WriteLine( "T" );
            }
        }
    }
}

// <target>
internal class Target
{
    [return: MyAspect]
    private string M1() => "foo";

    [return: MyAspect]
    private bool M2() => false;
}