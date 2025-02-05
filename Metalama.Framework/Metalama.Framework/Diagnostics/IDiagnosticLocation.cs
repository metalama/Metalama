// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Diagnostics
{
    /// <summary>
    /// A base interface for objects to which a diagnostic can be reported.
    /// </summary>
    /// <seealso href="@diagnostics"/>
    [CompileTime]
    [InternalImplement]
    public interface IDiagnosticLocation;
}