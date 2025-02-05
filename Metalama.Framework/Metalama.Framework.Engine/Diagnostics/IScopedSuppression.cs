// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Diagnostics;

public interface IScopedSuppression
{
    ISuppression Suppression { get; }

    ISymbol? GetScopeSymbolOrNull( CompilationContext compilationContext );
}