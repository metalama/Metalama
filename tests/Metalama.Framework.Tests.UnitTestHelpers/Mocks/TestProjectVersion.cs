// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime;
using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Tests.UnitTestHelpers.Mocks;

public sealed class TestProjectVersion : IProjectVersion
{
    private readonly Dictionary<string, ulong> _hashes;

    public TestProjectVersion(
        string assemblyName,
        Dictionary<string, ulong>? hashes = null,
        IProjectVersion[]? referencedCompilations = null ) : this(
        ProjectKeyFactory.CreateTest( assemblyName ),
        hashes,
        referencedCompilations ) { }

    public TestProjectVersion(
        ProjectKey assemblyIdentity,
        Dictionary<string, ulong>? hashes = null,
        IProjectVersion[]? referencedCompilations = null )
    {
        this._hashes = hashes ?? new Dictionary<string, ulong>();
        this.ProjectKey = assemblyIdentity;

        this.Compilation = CSharpCompilation.Create(
            assemblyIdentity.AssemblyName,
            hashes?.SelectAsArray( p => CSharpSyntaxTree.ParseText( "", path: p.Key, options: SupportedCSharpVersions.DefaultParseOptions ) ) );

        this.ReferencedProjectVersions = referencedCompilations?.ToImmutableDictionary( c => c.ProjectKey, c => c )
                                         ?? ImmutableDictionary<ProjectKey, IProjectVersion>.Empty;
    }

    public TestProjectVersion( Compilation compilation )
    {
        this.ProjectKey = compilation.GetProjectKey();
        this.Compilation = compilation;

        this.ReferencedProjectVersions = compilation.References.OfType<CompilationReference>()
            .ToImmutableDictionary( r => r.Compilation.GetProjectKey(), c => (IProjectVersion) new TestProjectVersion( c.Compilation ) );

#pragma warning disable CA1307
        this._hashes = compilation.SyntaxTrees.ToDictionary( t => t.FilePath, t => (ulong) t.GetRoot().ToFullString().GetHashCode() );
#pragma warning restore CA1307
    }

    public ProjectKey ProjectKey { get; }

    public bool TryGetSyntaxTreeVersion( string path, out SyntaxTreeVersion syntaxTreeVersion )
    {
        if ( this._hashes.TryGetValue( path, out var hash ) )
        {
            syntaxTreeVersion = new SyntaxTreeVersion( null!, false, hash );

            return true;
        }
        else
        {
            syntaxTreeVersion = default;

            return false;
        }
    }

    public Compilation Compilation { get; }

    public ImmutableDictionary<ProjectKey, IProjectVersion> ReferencedProjectVersions { get; }
}