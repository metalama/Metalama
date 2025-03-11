// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS0649

namespace Metalama.Framework.Tests.AspectTests.Aspects.IncrementInRuntimeConditionalBlock;

internal class AutoIncrementAttribute : OverrideFieldOrPropertyAspect
{
    [Introduce]
    private int oldValue;

    public override dynamic? OverrideProperty
    {
        get
        {
            var property = meta.Target.Property;

            if (oldValue != property.Value)
            {
                property.Value = property.Value + 1;
                property.Value += 1;
                property.Value++;
                ++property.Value;
            }

            return meta.Proceed();
        }
        set => throw new NotImplementedException();
    }
}

internal class TargetCode
{
    // <target>
    [AutoIncrementAttribute]
    private int Property { get; set; }
}