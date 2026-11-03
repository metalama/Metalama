// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Services;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace Metalama.Testing.AspectTesting;

/// <summary>
/// Remembers the output files written during a test run, so that a file that several tests produce is written only once.
/// For instance, two tests that include the same file with <c>// @Include</c> both produce the HTML of this file, and they
/// would fail if they wrote it concurrently. The test executor creates one instance per run, and the tests run concurrently,
/// so this class is thread-safe.
/// </summary>
internal sealed class TestOutputFileRegistry : IGlobalService
{
    private readonly ConcurrentDictionary<string, bool> _files = new( StringComparer.OrdinalIgnoreCase );

    /// <summary>
    /// Records that the caller writes a file, and returns <c>false</c> if another test of the run already wrote it.
    /// </summary>
    public bool TryAdd( string path ) => this._files.TryAdd( Path.GetFullPath( path ), true );
}
