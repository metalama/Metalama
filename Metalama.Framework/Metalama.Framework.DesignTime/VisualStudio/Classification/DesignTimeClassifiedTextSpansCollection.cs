// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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