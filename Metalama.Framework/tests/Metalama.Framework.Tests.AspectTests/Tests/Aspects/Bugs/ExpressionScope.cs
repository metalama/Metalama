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