// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Diagnostics
{
    /// <summary>
    /// An exception thrown by Metalama, embedding a <see cref="Diagnostic"/>, thrown in a situation where
    /// the responsibility can be put on the user. This exception type is typically not observed out of Metalama code,
    ///  and should be handled properly.
    /// </summary>
    internal sealed class DiagnosticException : Exception
    {
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
    }
}