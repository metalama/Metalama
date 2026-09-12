// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Immutable;
using System.IO;

namespace Metalama.Backstage.Diagnostics;

internal sealed class ConsoleLoggerFactory : SimpleLoggerFactory
{
    private readonly TextWriter _textWriter;

    public ConsoleLoggerFactory( string productName, TextWriter textWriter, ImmutableHashSet<string> traceCategories, bool includeThreadId = true ) : base(
        productName,
        traceCategories,
        includeThreadId )
    {
        this._textWriter = textWriter;
    }

    public override void Flush()
    {
        lock ( this._textWriter )
        {
            this._textWriter.Flush();
        }
    }

    protected override void Write( string message )
    {
        lock ( this._textWriter )
        {
            this._textWriter.WriteLine( message );
        }
    }
}