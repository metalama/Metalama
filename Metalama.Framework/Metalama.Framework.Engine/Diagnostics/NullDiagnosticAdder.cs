// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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