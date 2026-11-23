// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using Workspace = Metalama.Framework.Workspaces.Workspace;

namespace Metalama.Framework.Tests.Workspaces;

/// <summary>
/// Tests <see cref="Workspace.RemoveMetalamaSourceGenerator"/>, which removes the analyzer reference of the Metalama source
/// generator from the projects of a workspace and keeps the other analyzer references.
/// </summary>
public sealed class SourceGeneratorFilterTests
{
    [Fact]
    public void OnlyTheMetalamaSourceGeneratorIsRemoved()
    {
        var directory = Path.Combine( Path.GetTempPath(), "metalama-analyzers" );
        var metalama = CreateReference( Path.Combine( directory, "Metalama.Framework.CompilerExtensions.dll" ) );
        var metalamaAnalyzers = CreateReference( Path.Combine( directory, "Metalama.Framework.Analyzers.dll" ) );
        var other = CreateReference( Path.Combine( directory, "System.Text.RegularExpressions.Generator.dll" ) );

        using var workspace = new AdhocWorkspace();

        var solution = workspace.CurrentSolution
            .AddProject( "First", "First", LanguageNames.CSharp )
            .WithAnalyzerReferences( [metalama, metalamaAnalyzers, other] )
            .Solution
            .AddProject( "Second", "Second", LanguageNames.CSharp )
            .WithAnalyzerReferences( [other] )
            .Solution;

        var filtered = Workspace.RemoveMetalamaSourceGenerator( solution );

        var first = filtered.Projects.Single( p => p.Name == "First" );
        var second = filtered.Projects.Single( p => p.Name == "Second" );

        Assert.Equal( [metalamaAnalyzers, other], first.AnalyzerReferences );
        Assert.Equal( [other], second.AnalyzerReferences );
    }

    private static AnalyzerFileReference CreateReference( string path ) => new( path, NullAnalyzerAssemblyLoader.Instance );

    /// <summary>
    /// An analyzer assembly loader that never loads an assembly. The test reads only the paths of the references.
    /// </summary>
    private sealed class NullAnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
    {
        public static NullAnalyzerAssemblyLoader Instance { get; } = new();

        public void AddDependencyLocation( string fullPath ) { }

        public Assembly LoadFromPath( string fullPath ) => throw new NotSupportedException();
    }
}
