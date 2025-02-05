// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Pipeline.DesignTime;

public interface IDependencyCollector : IProjectService
{
    void AddDependency( INamedTypeSymbol masterSymbol, INamedTypeSymbol dependentSymbol );

    void AddDependency( INamedTypeSymbol masterSymbol, SyntaxTree dependentTree );

    void AddDependency( SyntaxTree masterTree, SyntaxTree dependentTree );
}