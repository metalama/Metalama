using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.ForEachTests.ForEachCompileTimeConditional;

#pragma warning disable CS0169

[CompileTime]
internal class Aspect
{
    [TestTemplate]
    private dynamic? Template()
    {
        if (meta.This.logMembers)
        {
            foreach (var field in meta.Target.Type.FieldsAndProperties)
            {
                Console.WriteLine( $"{field.Name} = {field.Value}" );
            }
        }

        return meta.Proceed();
    }
}

internal class TargetCode
{
    private bool logMembers;

    private int Method( int a, int bb )
    {
        return a + bb;
    }
}