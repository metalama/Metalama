// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Preview.BasicTest.Inheritance;

[Inheritable]
public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.ImplementInterface( typeof(IDisposable), whenExists: OverrideStrategy.Ignore );
        builder.IntroduceMethod( nameof(ProtectedDispose), whenExists: OverrideStrategy.Override );
    }

    [InterfaceMember]
    public virtual void Dispose()
    {
        meta.Proceed();
    }

    [Template(Name="Dispose")]
    protected virtual void ProtectedDispose( bool disposing )
    {
        meta.Proceed();
    }
}

[TheAspect]
public class BaseClass
{
    
}