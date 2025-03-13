// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.IO;

namespace Metalama.Backstage.Utilities;

/// <summary>
/// An exception thrown by <see cref="RetryHelper"/> when an operation failed and a process is detected that locks a file.
/// </summary>
public sealed class LockedFileException : IOException
{
    public LockedFileException( string message, Exception innerException ) : base( message, innerException ) { }
}