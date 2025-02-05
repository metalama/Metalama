// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.CodeModel.Abstractions
{
    internal interface ISdkDeclaration : IDeclaration
    {
        /// <summary>
        /// Gets the Roslyn <see cref="Location"/> of the declaration, to emit diagnostics.
        /// </summary>
        Location? DiagnosticLocation { get; }
    }
}