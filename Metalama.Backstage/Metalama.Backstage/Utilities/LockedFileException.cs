// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.IO;

namespace Metalama.Backstage.Utilities;

/// <summary>
/// An exception thrown by <see cref="RetryHelper"/> when an operation failed and a process is detected that locks a file.
/// </summary>
public class LockedFileException : IOException
{
    public LockedFileException( string message, Exception innerException ) : base( message, innerException ) { }
}