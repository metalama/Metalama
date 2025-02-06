// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System.Diagnostics;

namespace Flashtrace.Contexts;

/// <summary>
/// Represents a position in a file of source code.
/// </summary>
[PublicAPI]
public readonly struct SourceLineInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceLineInfo"/> struct.
    /// </summary>
    /// <param name="file">Path to the source code file.</param>
    /// <param name="line">Line in <paramref name="file"/>.</param>
    /// <param name="column">Column in <paramref name="file"/>.</param>
    [DebuggerStepThrough]
    public SourceLineInfo( string? file, int line, int column )
    {
        this.File = file;
        this.Line = line;
        this.Column = column;
    }

    /// <summary>
    /// Gets the path to the source code file.
    /// </summary>
    public string? File { get; }

    /// <summary>
    /// Gets the line in <see cref="File"/>.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Gets the column in <see cref="File"/>.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Gets a value indicating whether the current <see cref="SourceLineInfo"/> is null.
    /// </summary>
    public bool IsNull => this.File == null;
}