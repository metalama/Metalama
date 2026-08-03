// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Metalama.Framework.Engine.Diagnostics
{
    /// <summary>
    /// An exception thrown by Metalama, embedding a <see cref="Diagnostic"/>, thrown in a situation where
    /// the responsibility can be put on the user. This exception type is typically not observed out of Metalama code,
    ///  and should be handled properly.
    /// </summary>
    /// <remarks>
    /// The type is public, although it cannot be instantiated outside of Metalama, because the exception handlers of the
    /// design-time assemblies must recognize it in order to report the diagnostics that it carries instead of a crash.
    /// </remarks>
    public sealed class DiagnosticException : Exception
    {
        private static readonly Regex _whitespaceRegex = new( @"\s+", RegexOptions.CultureInvariant );

        public ImmutableArray<Diagnostic> Diagnostics { get; }

        /// <summary>
        /// Gets a value indicating whether the diagnostics should be attributed to source code.
        /// </summary>
        public bool InSourceCode { get; }

        internal DiagnosticException( string message, ImmutableArray<Diagnostic> diagnostics, bool inSourceCode = true ) : base(
            GetMessage( message, diagnostics ) )
        {
            this.Diagnostics = diagnostics;
            this.InSourceCode = inSourceCode;
        }

        internal DiagnosticException( Diagnostic diagnostic )
            : base( diagnostic.ToString() )
        {
            this.Diagnostics = ImmutableArray.Create( diagnostic );
            this.InSourceCode = true;
        }

        private static string GetMessage( string message, IReadOnlyList<Diagnostic> diagnostics )
            => message + Environment.NewLine + string.Join( Environment.NewLine, diagnostics.Where( d => d.Severity == DiagnosticSeverity.Error ) );

        /// <summary>
        /// Returns <see cref="Exception.Message"/> folded onto a single line, for a log record.
        /// </summary>
        /// <remarks>
        /// The message concatenates the diagnostics with <see cref="Environment.NewLine"/>, and a diagnostic message
        /// may itself contain a line break, so logging it as it is produces a multi-line log record. The text is not
        /// truncated: a log record is the only remaining trace of a failure that is deliberately not reported as a
        /// crash, so its length is preferable to its loss.
        /// </remarks>
        public string GetSingleLineMessage() => _whitespaceRegex.Replace( this.Message, " " ).Trim();

        /// <summary>
        /// Returns the <see cref="DiagnosticException"/> carried by <paramref name="exception"/>, or <c>null</c> when
        /// <paramref name="exception"/> does not represent a user-attributable failure.
        /// </summary>
        /// <remarks>
        /// A <see cref="DiagnosticException"/> thrown deep in the pipeline reaches the exception handlers wrapped in the
        /// exception types that the intermediate infrastructure adds, typically an <see cref="AggregateException"/> when
        /// the pipeline was invoked synchronously from a design-time entry point. Only these wrappers are unwrapped: an
        /// exception of any other type means that a defect intervened between the diagnostic and the handler, and such a
        /// failure must still be reported as a crash.
        /// </remarks>
        public static DiagnosticException? TryFind( Exception exception )
            => exception switch
            {
                DiagnosticException diagnosticException => diagnosticException,
                AggregateException { InnerExceptions.Count: 1 } aggregateException => TryFind( aggregateException.InnerExceptions[0] ),
                TargetInvocationException { InnerException: { } innerException } => TryFind( innerException ),
                _ => null
            };
    }
}