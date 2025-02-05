#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
#endif

#if ROSLYN_4_8_0_OR_GREATER

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

#pragma warning disable CS0169
#pragma warning disable CS0414

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Constructors.Attributes_Primary;

/*
 * Tests that overriding a primary constructor keeps all the existing attributes.
 */

public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.With( builder.Target.Constructors.Single() ).Override( nameof(Template) );
    }

    [Template]
    public void Template()
    {
        Console.WriteLine( "This is the overridden constructor." );
    }
}

[AttributeUsage( AttributeTargets.Constructor )]
public class ConstructorOnlyAttribute : Attribute { }

[AttributeUsage( AttributeTargets.Parameter )]
public class ParamOnlyAttribute : Attribute { }

// <target>
[Override]
[method: ConstructorOnly]
internal class TargetClass( [ParamOnly] int x )
{
    private int Z = x;
}

#endif