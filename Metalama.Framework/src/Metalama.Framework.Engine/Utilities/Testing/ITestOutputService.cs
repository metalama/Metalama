// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Services;
using System;

namespace Metalama.Framework.Engine.Utilities.Testing;

/// <summary>
/// A service that allows writing output to the test output stream during test execution.
/// This service is only available when running inside a test context.
/// </summary>
[Obsolete( "ITestOutputService must not be used in production code. Remove the code before committing." )]
public interface ITestOutputService : IGlobalService
{
    /// <summary>
    /// Writes a message to the test output.
    /// </summary>
    /// <param name="message">The message to write.</param>
    [Obsolete( "ITestOutputService must not be used in production code. Remove the code before committing." )]
    void WriteLine( string message );
}