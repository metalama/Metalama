using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using System;
using System.Linq;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Properties.Record_PositionalExplicit_Initialized;

[assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(ApplyAspect), typeof(MyAspect))]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Properties.Record_PositionalExplicit_Initialized;

internal class MyAspect : OverrideFieldOrPropertyAspect
{
    public override dynamic? OverrideProperty
    {
        get => meta.Proceed();
        set => meta.Proceed();
    }
}

// <target>
[ApplyAspect]
internal record MyRecord( int A, int B )
{
    public int A { get; init; } = A;

    private int _b = B;

    public int B
    {
        get
        {
            Console.WriteLine( "Original." );

            return _b;
        }
        init
        {
            Console.WriteLine( "Original." );
        }
    }
}

internal class ApplyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var p in builder.Target.Properties.Where( p => !p.IsImplicitlyDeclared ))
        {
            builder.With( p ).AddAspect<MyAspect>();
        }
    }
}