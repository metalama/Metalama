// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.ExpressionScope;

internal class NotNullAttribute : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );

        foreach (var parameter in builder.Target.Parameters.Where(
                     p => p.RefKind is RefKind.None or RefKind.In
                          && !p.Type.IsNullable.GetValueOrDefault()
                          && p.Type.IsReferenceType.GetValueOrDefault() ))
        {
            builder.With( parameter ).AddContract( nameof(Validate), args: new { parameterName = parameter.Name } );
        }
    }

    [Template]
    private void Validate( dynamic? value, [CompileTime] string parameterName )
    {
        if (value == null)
        {
            throw new ArgumentNullException( parameterName );
        }
    }
}

// <target>
internal class C
{
    [NotNull]
    public void M( string s ) { }
}