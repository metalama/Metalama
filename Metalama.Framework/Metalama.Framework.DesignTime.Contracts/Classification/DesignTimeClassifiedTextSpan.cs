// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis.Text;
using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.Classification
{
    /// <summary>
    /// Represents a <see cref="TextSpan"/> assigned to a classification.
    /// </summary>
    [Guid( "114cc8b6-7363-438c-8742-f3076bd8afce" )]
    [PublicAPI]
    public struct DesignTimeClassifiedTextSpan
    {
        /// <summary>
        /// Gets the <see cref="TextSpan"/>.
        /// </summary>
        public TextSpan Span;

        /// <summary>
        /// Gets the classification of <see cref="Span"/>.
        /// </summary>
        public DesignTimeTextSpanClassification Classification;
    }
}