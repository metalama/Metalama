using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Constructors.Contract;

/*
 * Tests single OverrideConstructor advice with contracts.
 */

public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.With( builder.Target.Constructors.Single().Parameters.Single() ).AddContract( nameof(InitializerTemplate), args: new { i = 1 } );
        builder.With( builder.Target.Constructors.Single() ).Override( nameof(Template), args: new { i = 1 } );
        builder.With( builder.Target.Constructors.Single().Parameters.Single() ).AddContract( nameof(InitializerTemplate), args: new { i = 2 } );
        builder.With( builder.Target.Constructors.Single() ).Override( nameof(Template), args: new { i = 2 } );
    }

    [Template]
    public void Template( [CompileTime] int i )
    {
        Console.WriteLine( $"This is the override {i}." );
        meta.Proceed();
    }

    [Template]
    public void InitializerTemplate( [CompileTime] int i, dynamic? value )
    {
        Console.WriteLine( $"This is the contract {i}." );
    }
}

// <target>
[Override]
public class TargetClass
{
    public TargetClass( int p )
    {
        Console.WriteLine( $"This is the original constructor." );
    }
}