// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Extensions.DependencyInjection.AspectTests.Misc.SkipAspect;

// Tests compatibility of Dependency with skip aspect.

public interface IMyInterface
{
    void Foo();
}

[AttributeUsage( AttributeTargets.Method | AttributeTargets.Property )]
public class TestAttribute : FieldOrPropertyAspect
{
    [IntroduceDependency]
    private readonly IMyInterface _testDependency;

    public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
    {
        if ( !builder.Target.Attributes.OfAttributeType( typeof( DisableAspectAttribute ) ).Any() )
        {
            builder.Advice.Override( builder.Target, nameof( this.OverrideProperty ) );
        }
        else
        {
            builder.SkipAspect();
        }
    }

    [Template]
    public dynamic? OverrideProperty
    {
        get
        {
            this._testDependency.Foo();
            return meta.Proceed();
        }
        set
        {
            this._testDependency.Foo();
            meta.Proceed();
        }
    }
}

[AttributeUsage( AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property )]
public sealed class DisableAspectAttribute : Attribute
{
}

// <target>
public class TargetClass
{
    [Test]
    [DisableAspect]
    public DateTime Property1 { get; set; }

    [Test]
    public double Property2 { get; set; }
}