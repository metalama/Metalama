// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Inheritance.Exclude;

[Inheritable]
internal class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var m in builder.Target.Methods)
        {
            builder.With( m ).Override( nameof(Template) );
        }
    }

    [Template]
    private dynamic? Template()
    {
        Console.WriteLine( "Overridden!" );

        return meta.Proceed();
    }
}

// <target>
internal class Targets
{
    [Aspect]
    private class BaseClass
    {
        private void M() { }
    }

    private class DerivedClass : BaseClass
    {
        private void N() { }
    }

    [ExcludeAspect( typeof(Aspect) )]
    private class ExcludedDerivedClass : BaseClass
    {
        private void N() { }
    }
}