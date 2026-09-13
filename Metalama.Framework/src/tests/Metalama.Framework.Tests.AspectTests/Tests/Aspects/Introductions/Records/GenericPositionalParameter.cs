// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Collections.Generic;
using System.Text;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Records.GenericPositionalParameter;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // The positional parameter is a constructed generic type, so the synthesized members that carry it are
        // resolved to their symbol by comparing a constructed type and not a generic definition.
        var record = builder.IntroduceRecord(
            "Positional",
            buildRecord: r =>
            {
                r.Accessibility = Accessibility.Public;
                r.AddPositionalParameter( "Values", typeof(List<int>) );
            } );

        var target = builder.With( record.Declaration );

        target.IntroduceMethod( nameof(IntroducedDeconstruct), whenExists: OverrideStrategy.Override );
        target.IntroduceMethod( nameof(IntroducedPrintMembers), whenExists: OverrideStrategy.Override );
    }

    [Template( Name = "Deconstruct" )]
    public void IntroducedDeconstruct( out List<int> Values )
    {
        Values = default!;

        meta.Proceed();
    }

    [Template( Name = "PrintMembers" )]
    protected bool IntroducedPrintMembers( StringBuilder builder )
    {
        return meta.Proceed();
    }
}

// <target>
[Introduction]
public class TargetType { }
