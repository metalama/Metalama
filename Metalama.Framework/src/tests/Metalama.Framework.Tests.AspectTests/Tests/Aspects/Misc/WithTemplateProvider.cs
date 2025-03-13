// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.WithTemplateProvider;

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var templateProvider = TemplateProvider.FromInstance( new TemplateProviderImpl() );

        builder.Advice.WithTemplateProvider( templateProvider ).IntroduceProperty( builder.Target, nameof(TemplateProviderImpl.IntroducedProperty) );

        foreach (var property in builder.Target.Properties)
        {
            builder.Advice.WithTemplateProvider( templateProvider ).Override( property, nameof(TemplateProviderImpl.OverrideTemplate) );
        }
    }
}

internal class TemplateProviderImpl : ITemplateProvider
{
    [Template]
    public string? OverrideTemplate
    {
        get
        {
            Console.WriteLine( $"Getting {meta.Target.Type.Name}." );

            return meta.Proceed();
        }

        set
        {
            Console.WriteLine( $"Setting {meta.Target.Type.Name} to '{value}'." );
            meta.Proceed();
        }
    }

    [Template]
    public string IntroducedProperty
    {
        get
        {
            Console.WriteLine( $"Getting {meta.Target.Type.Name}." );

            return "IntroducedProperty";
        }

        set
        {
            Console.WriteLine( $"Setting {meta.Target.Type.Name} to '{value}'." );
        }
    }
}

// <target>
[MyAspect]
public class C
{
    private string? P { get; set; }
}