// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.CompileTime;

/// <summary>
/// The public interface for symbol classification.
/// </summary>
public interface ISymbolClassificationService : IProjectService
{
    ExecutionScope GetExecutionScope( ISymbol symbol );

    bool IsTemplate( ISymbol symbol );

    bool IsCompileTimeParameter( IParameterSymbol symbol );

    bool IsCompileTimeTypeParameter( ITypeParameterSymbol symbol );
}