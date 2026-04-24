// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Formatting
{
    /// <summary>
    /// Represents a <see cref="TextSpan"/> and its <see cref="TextSpanClassification"/>.
    /// </summary>
    [PublicAPI]
    public readonly struct ClassifiedTextSpan : IClassifiedTextSpan
    {
        /// <summary>
        /// Tag name for C# classification.
        /// </summary>
        public const string CSharpClassTagName = "csharp";

        /// <summary>
        /// Tag name for diagnostic annotation.
        /// </summary>
        public const string DiagnosticTagName = "diagnostic";

        /// <summary>
        /// Tag name for generating aspect.
        /// </summary>
        public const string GeneratingAspectTagName = "aspect";

        /// <summary>
        /// Tag name for title.
        /// </summary>
        public const string TitleTagName = "title";

        /// <summary>
        /// Gets the <see cref="TextSpan"/>.
        /// </summary>
        public TextSpan Span { get; }

        /// <summary>
        /// Gets the classification of <see cref="Span"/>.
        /// </summary>
        public TextSpanClassification Classification { get; }

        [UsedImplicitly]
        public ImmutableDictionary<string, string> Tags { get; }

        /// <inheritdoc />
        public Diagnostic? Diagnostic
        {
            get
            {
                if ( !this.Tags.TryGetValue( DiagnosticTagName, out var json ) || string.IsNullOrEmpty( json ) )
                {
                    return null;
                }

                var annotation = DiagnosticAnnotation.FromJson( json );

                if ( annotation == null )
                {
                    return null;
                }

                var descriptor = new DiagnosticDescriptor(
                    annotation.Id,
                    annotation.Message,
                    annotation.Message,
                    "Metalama",
                    annotation.Severity,
                    isEnabledByDefault: true );

                return Diagnostic.Create( descriptor, Location.None );
            }
        }

        /// <inheritdoc />
        public string? CSharpClassification => this.Tags.TryGetValue( CSharpClassTagName, out var value ) ? value : null;

        /// <inheritdoc />
        public string? Title => this.Tags.TryGetValue( TitleTagName, out var value ) ? value : null;

        /// <inheritdoc />
        public string? GeneratingAspect => this.Tags.TryGetValue( GeneratingAspectTagName, out var value ) ? value : null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassifiedTextSpan"/> struct.
        /// </summary>
        internal ClassifiedTextSpan( TextSpan span, TextSpanClassification classification, ImmutableDictionary<string, string>? tags )
        {
            this.Span = span;
            this.Classification = classification;
            this.Tags = tags ?? ImmutableDictionary<string, string>.Empty;
        }

        public override string ToString()
        {
            var s = this.Span.ToString().ReplaceOrdinal( "2147483647" /* int.Max */, "inf" ) + "=>" + this.Classification;

            if ( this.Tags.Any() )
            {
                s += " " + string.Join( ", ", this.Tags.SelectAsReadOnlyCollection( tag => $"{tag.Key}={tag.Value}" ) );
            }

            return s;
        }
    }
}