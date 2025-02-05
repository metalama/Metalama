// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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