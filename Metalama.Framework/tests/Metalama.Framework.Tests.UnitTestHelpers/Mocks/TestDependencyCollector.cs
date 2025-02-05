// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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