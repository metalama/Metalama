// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_TwoLevel_NonInheritable;

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(InitializerTemplate), InitializerKind.AfterLastInstanceConstructor );
    }

    [Template]
    private void InitializerTemplate()
    {
        Console.WriteLine( $"OnConstructed on {meta.Target.Type.Name}!" );
    }
}

// <target>
[TheAspect]
public class BaseClass
{
    public string Value { get; }

    public BaseClass( string value )
    {
        this.Value = value;
    }
}

// <target>
public class DerivedClass : BaseClass
{
    public int Extra { get; }

    public DerivedClass( string value, int extra ) : base( value )
    {
        this.Extra = extra;
    }
}
