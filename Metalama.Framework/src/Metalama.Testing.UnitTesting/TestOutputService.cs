// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Testing;
using Xunit.Abstractions;

namespace Metalama.Testing.UnitTesting;

#pragma warning disable CS0618 // Type or member is obsolete
internal sealed class TestOutputService : ITestOutputService
#pragma warning restore CS0618 // Type or member is obsolete
{
    private readonly ITestOutputHelper? _testOutputHelper;

    public TestOutputService( ITestOutputHelper? testOutputHelper )
    {
        this._testOutputHelper = testOutputHelper;
    }

    public void WriteLine( string message )
    {
        this._testOutputHelper?.WriteLine( message );
    }
}
