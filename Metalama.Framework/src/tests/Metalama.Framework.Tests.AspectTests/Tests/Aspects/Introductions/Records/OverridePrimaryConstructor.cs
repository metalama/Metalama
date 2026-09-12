// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Records.OverridePrimaryConstructor;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Overriding the primary constructor of a record replaces it by an explicit one, which is the same
        // replacement that an initializer placed before an instance constructor performs.
        var positional = builder.IntroduceRecord(
            "Positional",
            RecordKind.Class,
            buildRecord: r =>
            {
                r.Accessibility = Accessibility.Public;
                r.AddPositionalParameter( "Value", typeof(int) );
            } );

        var primaryConstructor = positional.Declaration.Constructors.Single( c => c.IsPrimary );

        builder.With( primaryConstructor ).Override( nameof(Template) );
    }

    [Template]
    public void Template()
    {
        Console.WriteLine( $"Constructing {meta.Target.Type.Name}." );
        meta.Proceed();
    }
}

// <target>
[Introduction]
public class TargetType { }
