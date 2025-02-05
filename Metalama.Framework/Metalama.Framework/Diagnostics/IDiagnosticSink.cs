// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Diagnostics
{
    /// <summary>
    /// A sink that reports diagnostics reported from user code.
    /// </summary>
    /// <seealso href="@diagnostics"/>
    [CompileTime]
    [PublicAPI]
    [InternalImplement]
    public interface IDiagnosticSink
    {
        /// <summary>
        /// Reports a parametric diagnostic by specifying its location.
        /// </summary>
        void Report( IDiagnostic diagnostic, IDiagnosticLocation? location, IDiagnosticSource source );

        /// <summary>
        /// Suppresses a diagnostic by specifying the declaration in which the suppression must be effective.
        /// </summary>
        /// <param name="suppression">The suppression.</param>
        /// <param name="scope">The declaration in which the diagnostic must be suppressed.</param>
        /// <param name="source"></param>
        void Suppress( ISuppression suppression, IDeclaration scope, IDiagnosticSource source );
    }
}