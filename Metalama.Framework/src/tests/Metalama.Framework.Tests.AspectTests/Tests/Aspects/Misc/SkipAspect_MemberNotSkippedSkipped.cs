// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.SkipAspect_MemberNotSkippedSkipped;

public interface IMyInterface
{
    void Foo();
}

public class TestAttribute : MethodAspect
{
    [Introduce(WhenExists = OverrideStrategy.Ignore)]
    private readonly IMyInterface _testDependency;

    public override void BuildAspect(IAspectBuilder<IMethod> builder)
    {
        if (!builder.Target.Attributes.OfAttributeType(typeof(DisableAspectAttribute)).Any())
        {
            builder.Advice.Override(builder.Target, nameof(this.OverrideMethod) );
        }
        else
        {
            builder.SkipAspect();
        }
    }

    [Template]
    public dynamic? OverrideMethod()
    {
        foreach(var field in meta.Target.Type.Fields)
        {
            Console.WriteLine(field.Name);
        }

        this._testDependency.Foo();
        return meta.Proceed();
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property)]
public sealed class DisableAspectAttribute : Attribute
{
}

// <target>
public class TargetClass
{
    [Test]
    public DateTime Method1() 
    {
        return default;
    }

    [Test]
    [DisableAspect]
    public double Method2()
    {
        return default;
    }
}