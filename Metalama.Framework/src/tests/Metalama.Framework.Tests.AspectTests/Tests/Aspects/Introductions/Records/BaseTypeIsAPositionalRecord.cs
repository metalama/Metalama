// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Records.BaseTypeIsAPositionalRecord;

public record BaseRecord( int X );

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // A record class that derives from a positional record has to pass arguments to the primary constructor of
        // its base, which the language writes in the base list.
        builder.IntroduceRecord(
            "Derived",
            buildRecord: r =>
            {
                r.Accessibility = Accessibility.Public;
                r.BaseType = (INamedType) TypeFactory.GetType( typeof(BaseRecord) );

                var x = r.AddPositionalParameter( "X", typeof(int) );
                r.AddPositionalParameter( "Y", typeof(int) );

                r.AddBaseArgument( x );
            } );
    }
}

// <target>
[Introduction]
public class TargetType { }
