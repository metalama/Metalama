// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// Non-virtual Initialize on a sealed class implementing IInitializable — no LAMA0655 error.

using Metalama.Framework.RunTime.Initialization;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_Linker_SealedNoWarning;

public sealed class MyType : IInitializable
{
    public int Value { get; init; }

    public void Initialize( InitializationContext context )
    {
    }
}

// <target>
public class Caller
{
    public void Method()
    {
        var t = new MyType { Value = 42 };
    }
}
