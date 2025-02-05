using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.ParameterAspect_;

public class RequiredAttribute : ParameterAspect
{
    public override void BuildAspect( IAspectBuilder<IParameter> builder )
    {
        builder.With( (IMethod)builder.Target.DeclaringMember ).Override( nameof(Template), tags: new { ParameterName = builder.Target.Name } );
    }

    [Template]
    private dynamic? Template()
    {
        var parameterName = (string)meta.Tags["ParameterName"]!;
        var parameter = ExpressionFactory.Parse( parameterName );

        if (parameter.Value == null)
        {
            throw new ArgumentNullException( parameterName );
        }

        return meta.Proceed();
    }
}

// <target>
internal class Class
{
    private void M( [Required] object? a, [Required] object? b ) { }
}