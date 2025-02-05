using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.ForEachTests.ForEachCompileTimeConditionalInvalid;

#pragma warning disable CS0169

[CompileTime]
internal class Aspect
{
    [TestTemplate]
    private dynamic? Template()
    {
        var fieldName = meta.CompileTime( "" );

        if (meta.This.logMembers)
        {
            foreach (var field in meta.Target.Type.FieldsAndProperties)
            {
                fieldName = field.Name;
            }
        }

        Console.WriteLine( fieldName );

        return meta.Proceed();
    }
}

internal class TargetCode
{
    private bool logMembers;

    private void Method() { }
}