using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Constructors.PrimaryRecordClass_ExplicitProperty;

/*
 * Tests single OverrideConstructor advice on a primary constructor of a record class with a explicitly defined positional property.
 */

public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var constructor in builder.Target.Constructors)
        {
            if (constructor.IsImplicitlyDeclared)
            {
                continue;
            }

            builder.With( constructor ).Override( nameof(Template) );
        }
    }

    [Template]
    public void Template()
    {
        Console.WriteLine( "This is the override." );

        foreach (var param in meta.Target.Parameters)
        {
            Console.WriteLine( $"Param {param.Name} = {param.Value}" );
        }

        meta.Proceed();
    }
}

// <target>
[Override]
public record class TargetClass( int X, int Y )
{
    public int X { get; set; } = X;

    public void Foo()
    {
        var (x, y) = this;
        _ = this with { X = 13, Y = 42 };
        X = 42;
    }
}