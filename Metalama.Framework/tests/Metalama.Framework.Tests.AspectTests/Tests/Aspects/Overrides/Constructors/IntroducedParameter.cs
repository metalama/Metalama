using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Constructors.IntroducedParameter;

/*
 * Tests single OverrideConstructor advice with with IntroduceParameter in a single aspect.
 */

public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var constructor in builder.Target.Constructors)
        {
            builder.With( constructor ).Override( nameof(Template), args: new { i = 1 } );
            builder.With( constructor ).IntroduceParameter( "introduced", TypeFactory.GetType( SpecialType.Int32 ), TypedConstant.Create( 42 ) );
            builder.With( constructor ).Override( nameof(Template), args: new { i = 2 } );
        }
    }

    [Template]
    public void Template( [CompileTime] int i )
    {
        Console.WriteLine( $"This is the override {i}." );

        foreach (var param in meta.Target.Parameters)
        {
            Console.WriteLine( $"Param {param.Name} = {param.Value}" );
        }

        meta.Proceed();
    }
}

// <target>
[Override]
public class TargetClass
{
    public TargetClass()
    {
        Console.WriteLine( $"This is the original constructor." );
    }

    public TargetClass( int x )
    {
        Console.WriteLine( $"This is the original constructor." );
    }
}