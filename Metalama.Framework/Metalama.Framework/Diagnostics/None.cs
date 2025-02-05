// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Diagnostics
{
    /// <summary>
    /// A type to be used as generic argument of <see cref="DiagnosticDefinition{T}"/> when there is no parameter in the message.
    /// </summary>
    [CompileTime]
    public readonly struct None;
}