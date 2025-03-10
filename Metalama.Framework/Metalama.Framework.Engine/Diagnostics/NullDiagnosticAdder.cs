// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Diagnostics
{
    public sealed class NullDiagnosticAdder : IDiagnosticAdder
    {
        private readonly ILogger _logger;

        public static NullDiagnosticAdder Instance { get; } = new();

        private NullDiagnosticAdder()
        {
            this._logger = Logger.LoggerFactory.GetLogger( "NullDiagnosticAdder" );
        }

        public void Report( Diagnostic diagnostic )
        {
            switch ( diagnostic.Severity )
            {
                case DiagnosticSeverity.Error:
                case DiagnosticSeverity.Warning:
                    this._logger.Warning?.Log( diagnostic.ToString() );

                    break;
            }
        }
    }
}