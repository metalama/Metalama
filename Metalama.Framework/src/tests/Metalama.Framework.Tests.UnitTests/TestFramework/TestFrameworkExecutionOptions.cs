// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.TestFramework;

internal sealed class TestFrameworkExecutionOptions : ITestFrameworkExecutionOptions
{
    public TValue GetValue<TValue>( string name )
        => name switch
        {
            "xunit.execution.MaxParallelThreads" => (TValue) (object) 4,
            "xunit.execution.DisableParallelization" => (TValue) (object) false,
            _ => throw new ArgumentOutOfRangeException()
        };

    public void SetValue<TValue>( string name, TValue value ) => throw new NotImplementedException();
}