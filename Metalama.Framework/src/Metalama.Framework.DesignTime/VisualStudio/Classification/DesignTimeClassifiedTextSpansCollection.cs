// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.Classification;
using Metalama.Framework.Engine.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace Metalama.Framework.DesignTime.VisualStudio.Classification;

internal sealed class DesignTimeClassifiedTextSpansCollection : IDesignTimeClassifiedTextCollection
{
    private readonly ClassifiedTextSpanCollection _underlying;

    public DesignTimeClassifiedTextSpansCollection( ClassifiedTextSpanCollection underlying )
    {
        this._underlying = underlying;
    }

    public DesignTimeClassifiedTextSpan[] GetClassifiedTextSpans()
        => this._underlying.SelectAsArray( x => new DesignTimeClassifiedTextSpan { Span = x.Span, Classification = x.Classification.ToDesignTime() } );

    public DesignTimeClassifiedTextSpan[] GetClassifiedTextSpans( int spanStart, int spanLength )
        => this._underlying.GetClassifiedSpans( new TextSpan( spanStart, spanLength ) )
            .Select( x => new DesignTimeClassifiedTextSpan { Span = x.Span, Classification = x.Classification.ToDesignTime() } )
            .ToArray();
}