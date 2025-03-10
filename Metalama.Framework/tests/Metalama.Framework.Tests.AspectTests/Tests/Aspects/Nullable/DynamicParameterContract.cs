// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @ClearIgnoredDiagnostics to verify nullability warnings
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Nullable.DynamicParameterContract;

internal class Aspect : ContractAspect
{
    public override void Validate( dynamic? value )
    {
        value?.ToString();
        value!.ToString();
    }
}

// <target>
internal class TargetCode
{
    private class Nullable
    {
        [Aspect]
        public string? Field = null;

        [Aspect]
        public string? Property { get; set; }

        [Aspect]
        public string? this[ int i ] => null;

        [return: Aspect]
        private string? Method( [Aspect] string? arg ) => arg;
    }

    private class NotNullable
    {
        [Aspect]
        public string Field = null!;

        [Aspect]
        public string Property { get; set; } = null!;

        [Aspect]
        public string this[ int i ] => null!;

        [return: Aspect]
        private string Method( [Aspect] string arg ) => arg;
    }

#nullable disable

    private class Oblivious
    {
        [Aspect]
        public string Field = null;

        [Aspect]
        public string Property { get; set; }

        [Aspect]
        public string this[ int i ] => null;

        [return: Aspect]
        private string Method( [Aspect] string arg ) => arg;
    }
}