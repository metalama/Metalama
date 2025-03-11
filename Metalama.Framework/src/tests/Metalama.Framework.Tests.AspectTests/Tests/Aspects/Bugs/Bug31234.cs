// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug31234;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OverrideAttribute), typeof(InitializerAttribute) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug31234;

public class InitializerAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer(
            nameof(InitializerTemplate),
            InitializerKind.BeforeInstanceConstructor,
            args: new { property = builder.Target.Properties.Single() } );
    }

    [Template]
    public void InitializerTemplate( [CompileTime] IProperty property )
    {
        meta.InsertComment( "Should invoke the first override since it changes semantics of the original declaration." );
        property.Value = 42;
    }
}

public class OverrideAttribute : OverrideFieldOrPropertyAspect
{
    public override dynamic? OverrideProperty
    {
        get
        {
            // Block inlining
            _ = meta.Proceed();

            return meta.Proceed();
        }
        set
        {
            meta.Proceed();
            meta.Proceed();
        }
    }
}

public abstract class BaseClass
{
    public abstract int AbstractBaseProperty { get; }
}

// <target>
[Initializer]
public partial class TargetClass : BaseClass
{
    [OverrideAttribute]
    public override int AbstractBaseProperty { get; }

    public TargetClass() { }
}