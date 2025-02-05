// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#if TEST_OPTIONS
// @TestScenario(DesignTime)
// @ReportOutputWarnings
#endif

using Metalama.Framework.Aspects;
using Metalama.Extensions.DependencyInjection;
using Metalama.Extensions.DependencyInjection.AspectTests.Advice.Programmatic_DesignTime;
using Metalama.Framework.Code;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(DependencyAttribute), typeof(MyAspect) )]

namespace Metalama.Extensions.DependencyInjection.AspectTests.Advice.Programmatic_DesignTime;

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceDependency( typeof( IFormatProvider ), new() { MemberName = "_formatProvider" } );
    }
}

// <target>
[MyAspect]
public partial class TargetClass
{
    public TargetClass() { }

    public TargetClass( int x, IFormatProvider existingParameter ) { }
}