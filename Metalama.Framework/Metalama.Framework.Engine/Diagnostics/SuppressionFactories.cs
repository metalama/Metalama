// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

namespace Metalama.Framework.Engine.Diagnostics;

public static class SuppressionFactories
{
    public static SuppressionDescriptor CreateDescriptor( string diagnosticId )
        => new( "Metalama." + diagnosticId, diagnosticId, justification: string.Empty );

    public static ISuppressibleDiagnostic CreateDiagnostic( Diagnostic diagnostic ) => new SuppressibleDiagnostic( diagnostic );

    private sealed class SuppressibleDiagnostic( Diagnostic diagnostic ) : ISuppressibleDiagnostic
    {
        public string Id { get; } = diagnostic.Id;

        [Memo]
        public string InvariantMessage => diagnostic.GetMessage( CultureInfo.InvariantCulture );

        [Memo]
        public ImmutableArray<object?> Arguments => this.ExtractArguments();

        private ImmutableArray<object?> ExtractArguments()
        {
            var recorder = new RecorderFormatProvider();
            diagnostic.GetMessage( recorder );
            
            return recorder.Arguments.ToImmutableArray();
        }

        [Memo]
        public SourceSpan? Span => this.GetSpan();

        private SourceSpan? GetSpan()
        {
            if ( diagnostic.Location.SourceTree is { } tree )
            {
                var sourceSpan = diagnostic.Location.SourceSpan;
                var lineSpan = tree.GetLineSpan( sourceSpan );
                var text = tree.GetText().ToString( sourceSpan );

                return new SourceSpan(
                    tree.FilePath,
                    tree,
                    sourceSpan.Start,
                    sourceSpan.End,
                    lineSpan.StartLinePosition.Line,
                    lineSpan.StartLinePosition.Character,
                    lineSpan.EndLinePosition.Line,
                    lineSpan.EndLinePosition.Character,
                    text );
            }

            return null;
        }
    }

    private sealed class RecorderFormatProvider : IFormatProvider, ICustomFormatter
    {
        public List<object?> Arguments { get; } = [];

        public object? GetFormat( Type? formatType )
        {
            if ( formatType == typeof(ICustomFormatter) )
            {
                return this;
            }

            return null;
        }

        public string Format( string? format, object? arg, IFormatProvider? formatProvider )
        {
            this.Arguments.Add( arg );

            return string.Empty;
        }
    }
}