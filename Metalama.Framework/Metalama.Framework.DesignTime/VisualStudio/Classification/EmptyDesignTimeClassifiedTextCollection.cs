// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Contracts.Classification;

namespace Metalama.Framework.DesignTime.VisualStudio.Classification;

internal sealed class EmptyDesignTimeClassifiedTextCollection : IDesignTimeClassifiedTextCollection
{
    public static EmptyDesignTimeClassifiedTextCollection Instance { get; } = new();

    private EmptyDesignTimeClassifiedTextCollection() { }

    public DesignTimeClassifiedTextSpan[] GetClassifiedTextSpans() => Array.Empty<DesignTimeClassifiedTextSpan>();

    public DesignTimeClassifiedTextSpan[] GetClassifiedTextSpans( int spanStart, int spanLength ) => Array.Empty<DesignTimeClassifiedTextSpan>();
}