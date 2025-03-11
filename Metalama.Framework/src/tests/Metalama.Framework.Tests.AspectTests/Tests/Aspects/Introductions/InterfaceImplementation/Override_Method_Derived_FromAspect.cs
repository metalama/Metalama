// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Override_Method_Derived_FromAspect;

[Inheritable]
public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        builder.ImplementInterface( typeof(IDisposable), whenExists: OverrideStrategy.Override );
    }

    [InterfaceMember]
    public virtual void Dispose()
    {
        meta.Proceed();
        Console.WriteLine( "TheAspect.Dispose()" );
    }
}

// <target>
[TheAspect]
internal class C { }

// <target>
internal class D : C { }