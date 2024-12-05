// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Testing.UnitTesting;

namespace Metalama.Framework.Tests.UnitTests.LamaSerialization;

public abstract partial class SerializationTestsBase
{
    protected sealed record SerializationTestContextOptions : TestContextOptions
    {
        public string Code { get; init; } = "";
    }
}