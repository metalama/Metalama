// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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