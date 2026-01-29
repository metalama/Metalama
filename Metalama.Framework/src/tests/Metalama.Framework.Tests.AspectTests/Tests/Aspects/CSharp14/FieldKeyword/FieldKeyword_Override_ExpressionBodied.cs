// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
#endif

#if ROSLYN_5_0_0_OR_GREATER

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.FieldKeyword_Override_ExpressionBodied;

/// <summary>
/// Tests overriding expression-bodied read-only properties with a field keyword template.
/// This makes the computed property settable while keeping the original expression as the source.
/// </summary>
internal class MakeSettableAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach ( var property in builder.Target.Properties )
        {
            builder.With( property ).Override( nameof(PropertyTemplate) );
        }
    }

    [Template]
    public int PropertyTemplate
    {
        get
        {
            Console.WriteLine( $"Getting {meta.Target.Property.Name}" );
            return field;
        }
        set
        {
            Console.WriteLine( $"Setting {meta.Target.Property.Name} to {value}" );
            field = value;
        }
    }
}

// <target>
[MakeSettableAspect]
internal class C
{
    private int _counter;

    public int ComputedValue => _counter * 2;

    public int ConstantValue => 42;
}

#endif
