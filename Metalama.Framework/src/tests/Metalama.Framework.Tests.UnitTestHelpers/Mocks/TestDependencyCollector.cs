// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Pipeline.DesignTime;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Metalama.Framework.Tests.UnitTestHelpers.Mocks;

public sealed class TestDependencyCollector : IDependencyCollector
{
    public HashSet<string> Dependencies { get; } = new();

    public void AddDependency( INamedTypeSymbol masterSymbol, INamedTypeSymbol dependentSymbol )
    {
        this.Dependencies.Add( $"{dependentSymbol}->{masterSymbol}" );
    }

    public void AddDependency( INamedTypeSymbol masterSymbol, SyntaxTree dependentTree )
    {
        this.Dependencies.Add( $"{dependentTree.FilePath}->{masterSymbol}" );
    }

    public void AddDependency( SyntaxTree masterTree, SyntaxTree dependentTree )
    {
        this.Dependencies.Add( $"{dependentTree.FilePath}->{masterTree.FilePath}" );
    }
}