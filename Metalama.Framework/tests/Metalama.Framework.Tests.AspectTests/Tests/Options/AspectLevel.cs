using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Options;
using Metalama.Framework.Code.DeclarationBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Options;

#if TEST_OPTIONS
// @Include(_Common.cs)
#endif

public class TheAspect : TypeAspect, IHierarchicalOptionsProvider
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var options = builder.Target.Enhancements().GetOptions<MyOptions>();

        builder.IntroduceAttribute( AttributeConstruction.Create( typeof(ActualOptionsAttribute), new[] { options.OverrideHistory } ) );
    }

    public IEnumerable<IHierarchicalOptions> GetOptions( in OptionsProviderContext context )
    {
        return new[] { new MyOptions { Value = "FromTheAspect" } };
    }
}

// <target>
[MyOptions( "FromAttribute" )]
[TheAspect]
public class C { }