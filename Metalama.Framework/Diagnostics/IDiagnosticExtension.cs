// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using System.Collections.Immutable;

namespace Metalama.Framework.Diagnostics;

[CompileTime]
public interface IDiagnosticExtension
{
    ImmutableDictionary<string, string?> ConfigureProperties( ImmutableDictionary<string, string?> properties );
}