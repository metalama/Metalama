// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Diagnostics;

[CompileTime]
[InternalImplement]
public interface IDiagnosticSource
{
    string DiagnosticSourceDescription { get; }
}