// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplatingCodeValidation.UseTemplateOnlyInCompileTimeOnly_Suggestions;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        meta.InsertStatement( GetLoggingExpression( meta.Target.Parameters[0] ) );

        return meta.Proceed();
    }

    private IExpression GetLoggingExpression( IParameter parameter )
    {
        var builder = new ExpressionBuilder();

        builder.AppendTypeName( typeof(Console) );
        builder.AppendVerbatim( ".WriteLine(\"this: {0}, {1}: {2}\", " );
        builder.AppendExpression( ExpressionFactory.This() );
        builder.AppendVerbatim( ", " );
        builder.AppendLiteral( parameter.Name );
        builder.AppendVerbatim( ", " );
        builder.AppendExpression( parameter );
        builder.AppendVerbatim( ")" );

        return builder.ToExpression();
    }
}

// <target>
internal class Target
{
    [Aspect]
    private void M( object obj ) { }
}