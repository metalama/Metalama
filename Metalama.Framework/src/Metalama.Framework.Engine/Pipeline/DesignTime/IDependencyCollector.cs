// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Pipeline.DesignTime;

public interface IDependencyCollector : IProjectService
{
    void AddDependency( INamedTypeSymbol masterSymbol, INamedTypeSymbol dependentSymbol );

    void AddDependency( INamedTypeSymbol masterSymbol, SyntaxTree dependentTree );

    void AddDependency( SyntaxTree masterTree, SyntaxTree dependentTree );
}