// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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