// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Xunit.Sdk;

namespace Metalama.Framework.Tests.UnitTests.TestFramework;

/// <summary>
/// An implementation of <see cref="ITestFrameworkExecutionOptions"/> in which every option has its default value.
/// </summary>
internal sealed class TestFrameworkExecutionOptions : ITestFrameworkExecutionOptions
{
    public TValue GetValue<TValue>( string name ) => default!;

    public void SetValue<TValue>( string name, TValue value ) => throw new NotSupportedException();

    public string ToJson() => "{}";
}
