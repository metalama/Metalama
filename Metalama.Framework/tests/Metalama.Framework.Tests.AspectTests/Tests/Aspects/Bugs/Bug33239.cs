// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug33239;

[Inheritable]
public sealed class TestAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var property in builder.Target.Properties)
        {
            builder.With( property ).OverrideAccessors( null, nameof(OverridePropertySetter) );
        }
    }

    [Template]
    private void OverridePropertySetter( dynamic value )
    {
        if (value != meta.Target.Property.Value)
        {
            meta.Proceed();
        }
    }
}

public interface IBase
{
    int X { get; set; }
}

// <target>
[TestAspect]
public partial class Imp : IBase
{
    int IBase.X { get; set; }
}