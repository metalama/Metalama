// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Override_Method_DerivedFromAbstract;

// Tests that an inherited abstract method can be overridden (#2148).

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        builder.ImplementInterface( typeof(IDisposable), whenExists: OverrideStrategy.Override );
    }

    [InterfaceMember]
    public void Dispose()
    {
        Console.WriteLine( "TheAspect.Dispose()" );
    }
}

internal abstract class A : IDisposable
{
    public abstract void Dispose();
}

// <target>
[TheAspect]
internal abstract class B : A { }
