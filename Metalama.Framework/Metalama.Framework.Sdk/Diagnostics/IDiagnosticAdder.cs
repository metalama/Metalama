// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Diagnostics
{
    /// <summary>
    /// Allows to report diagnostics.
    /// </summary>
    public interface IDiagnosticAdder
    {
        void Report( Diagnostic diagnostic );
    }
}