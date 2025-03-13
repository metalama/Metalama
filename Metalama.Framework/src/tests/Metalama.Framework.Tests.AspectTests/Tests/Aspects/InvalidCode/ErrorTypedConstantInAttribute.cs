// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.InvalidCode.ErrorTypedConstantInAttribute;

/*
 * Tests that errorneous expression in aspect attribute does not cause an exception.
 */

internal class Aspect : OverrideFieldOrPropertyAspect
{
    private int constructorValue;

    public Aspect(int constructorValue)
    {
        this.constructorValue = constructorValue;
    }

    public Aspect()
    {
    }

    public int PropertyValue { get; }

    public override dynamic? OverrideProperty
    {
        get
        {
            Console.WriteLine($"ConstructorValue: {this.constructorValue}");
            Console.WriteLine($"PropertyValue: {this.PropertyValue}");
            return meta.Proceed();
        }
        set
        {
            meta.Proceed();
        }
    }
}

// <target>
internal class TargetCode
{
#if TESTRUNNER
    [Aspect(int.Parse("42"))]
#endif
    public int Foo { get; set; }

#if TESTRUNNER
    [Aspect(PropertyValue = int.Parse("42"))]
#endif
    public int Bar { get; set; }
}