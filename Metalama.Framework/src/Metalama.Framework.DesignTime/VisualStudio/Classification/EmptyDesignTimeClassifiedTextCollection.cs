// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.Classification;

namespace Metalama.Framework.DesignTime.VisualStudio.Classification;

internal sealed class EmptyDesignTimeClassifiedTextCollection : IDesignTimeClassifiedTextCollection
{
    public static EmptyDesignTimeClassifiedTextCollection Instance { get; } = new();

    private EmptyDesignTimeClassifiedTextCollection() { }

    public DesignTimeClassifiedTextSpan[] GetClassifiedTextSpans() => Array.Empty<DesignTimeClassifiedTextSpan>();

    public DesignTimeClassifiedTextSpan[] GetClassifiedTextSpans( int spanStart, int spanLength ) => Array.Empty<DesignTimeClassifiedTextSpan>();
}