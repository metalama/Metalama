using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using System;
using System.Linq;

using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Properties.Record_PositionalExplicit_Modified;

[assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(ApplyAspect), typeof(MyAspect))]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Properties.Record_PositionalExplicit_Modified;

internal class MyAspect : OverrideFieldOrPropertyAspect
{
    public override dynamic? OverrideProperty
    {
        get
        {
            Console.WriteLine( "MyAspect" );

            return meta.Proceed();
        }
        set
        {
            Console.WriteLine( "MyAspect" );
            meta.Proceed();
        }
    }
}

#pragma warning disable CS8907 // Parameter is unread. Did you forget to use it to initialize the property with that name?

// <target>
[ApplyAspect]
internal record MyRecord( int A, int B )
{
    public int A { get; init; }

    public int B
    {
        get
        {
            Console.WriteLine( "Original." );

            return 42;
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