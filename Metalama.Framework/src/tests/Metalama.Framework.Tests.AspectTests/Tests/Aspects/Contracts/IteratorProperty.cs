// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.IteratorProperty;

using System;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

public sealed class TestAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        foreach (var property in builder.Target.Properties)
        {
            builder.With( property )
                .AddContract(
                    nameof(ValidateParameter),
                    direction: ContractDirection.Input );

            // #32616
            //builder.With( //    property ).AddContract(
            //    nameof(ValidateParameter),
            //    direction: ContractDirection.Output);
        }
    }

    [Template]
    private void ValidateParameter( dynamic? value )
    {
        if (value is null)
        {
            throw new ArgumentNullException();
        }
    }
}

// <target>
[Test]
public class TestClass
{
    public IEnumerable<string> Enumerable
    {
        get
        {
            yield return "Hello";
        }
        set { }
    }

    public IEnumerator<string> Enumerator
    {
        get
        {
            yield return "Hello";
        }
        set { }
    }
}