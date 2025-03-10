// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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