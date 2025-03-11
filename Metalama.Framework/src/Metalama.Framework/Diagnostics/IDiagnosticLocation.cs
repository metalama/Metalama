// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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