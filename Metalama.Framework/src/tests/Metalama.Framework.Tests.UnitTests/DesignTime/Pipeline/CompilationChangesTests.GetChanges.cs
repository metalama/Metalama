// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime;
using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.DesignTime.Pipeline.Diff;
using Metalama.Framework.DesignTime.Rpc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

public sealed partial class CompilationChangesTests
{
    private CompilationChanges CompareSyntaxTrees( Compilation compilation1, Compilation compilation2 )
        => CompilationChanges.Incremental(
            ProjectVersion.Create( compilation1, compilation1.GetProjectKey(), this._strategy ),
            compilation2,
            new ReferenceChanges(
                ImmutableDictionary<ProjectKey, IProjectVersion>.Empty,
                ImmutableDictionary<ProjectKey, ReferencedProjectChange>.Empty,
                ImmutableHashSet<string>.Empty,
                ImmutableDictionary<string, ReferenceChangeKind>.Empty ) );

    [Fact]
    public void AddSyntaxTree_Standard()
    {
        using var testContext = this.CreateTestContext();
        var code = new Dictionary<string, string>();
        var compilation1 = testContext.CreateCSharpCompilation( code, assemblyName: nameof(this.AddSyntaxTree_Standard) );

        code.Add( "code.cs", "class C { }" );

        var compilation2 = testContext.CreateCSharpCompilation( code, assemblyName: nameof(this.AddSyntaxTree_Standard) );
        var changes = this.CompareSyntaxTrees( compilation1, compilation2 );

        Assert.False( changes.HasCompileTimeCodeChange );
        Assert.True( changes.HasChange );
        Assert.True( changes.IsIncremental );
        Assert.Equal( compilation2, changes.NewProjectVersion.CompilationToAnalyze );
        Assert.Single( changes.SyntaxTreeChanges );

        var syntaxTreeChange = changes.SyntaxTreeChanges.Single().Value;

        Assert.Equal( SyntaxTreeChangeKind.Added, syntaxTreeChange.SyntaxTreeChangeKind );
        Assert.Equal( CompileTimeChangeKind.None, syntaxTreeChange.CompileTimeChangeKind );
        Assert.True( syntaxTreeChange.OldSyntaxTreeVersionDangerous.IsDefault );
        Assert.NotNull( syntaxTreeChange.NewTree );
    }

    [Fact]
    public void AddSyntaxTree_CompileTime()
    {
        using var testContext = this.CreateTestContext();

        var code = new Dictionary<string, string>();
        var compilation1 = testContext.CreateCSharpCompilation( code );

        code.Add( "code.cs", "using Metalama.Framework.Aspects;  class C { }" );

        var compilation2 = testContext.CreateCSharpCompilation( code );
        var changes = this.CompareSyntaxTrees( compilation1, compilation2 );

        Assert.True( changes.HasChange );
        Assert.True( changes.HasCompileTimeCodeChange );
        Assert.True( changes.IsIncremental );
        Assert.Equal( compilation2, changes.NewProjectVersion.CompilationToAnalyze );
        Assert.Single( changes.SyntaxTreeChanges );

        var syntaxTreeChange = changes.SyntaxTreeChanges.Single().Value;

        Assert.Equal( SyntaxTreeChangeKind.Added, syntaxTreeChange.SyntaxTreeChangeKind );
        Assert.Equal( CompileTimeChangeKind.NewlyCompileTime, syntaxTreeChange.CompileTimeChangeKind );
        Assert.True( syntaxTreeChange.OldSyntaxTreeVersionDangerous.IsDefault );
        Assert.NotNull( syntaxTreeChange.NewTree );
    }

    [Fact]
    public void AddSyntaxTree_PartialType()
    {
        using var testContext = this.CreateTestContext();

        var code = new Dictionary<string, string>();
        var compilation1 = testContext.CreateCSharpCompilation( code, assemblyName: nameof(this.AddSyntaxTree_PartialType) );

        code.Add( "code.cs", "partial class C { }" );

        var compilation2 = testContext.CreateCSharpCompilation( code, assemblyName: nameof(this.AddSyntaxTree_PartialType) );
        var changes = this.CompareSyntaxTrees( compilation1, compilation2 );

        Assert.True( changes.HasChange );
        Assert.False( changes.HasCompileTimeCodeChange );
        Assert.True( changes.IsIncremental );
        Assert.Equal( compilation2, changes.NewProjectVersion.CompilationToAnalyze );
        Assert.Single( changes.SyntaxTreeChanges );

        var syntaxTreeChange = changes.SyntaxTreeChanges.Single().Value;

        Assert.Equal( SyntaxTreeChangeKind.Added, syntaxTreeChange.SyntaxTreeChangeKind );
        Assert.Equal( CompileTimeChangeKind.None, syntaxTreeChange.CompileTimeChangeKind );
        Assert.Single( syntaxTreeChange.PartialTypeChanges );
        Assert.Equal( PartialTypeChangeKind.Added, syntaxTreeChange.PartialTypeChanges[0].Kind );
    }

    [Fact]
    public void RemoveSyntaxTree()
    {
        using var testContext = this.CreateTestContext();
        var code = new Dictionary<string, string> { { "code.cs", "class C { }" } };
        var compilation1 = testContext.CreateCSharpCompilation( code, assemblyName: nameof(this.RemoveSyntaxTree) );

        code.Clear();

        var compilation2 = testContext.CreateCSharpCompilation( code, assemblyName: nameof(this.RemoveSyntaxTree) );

        var changes = this.CompareSyntaxTrees( compilation1, compilation2 );

        Assert.True( changes.HasChange );
        Assert.False( changes.HasCompileTimeCodeChange );
        Assert.True( changes.IsIncremental );
        Assert.Equal( compilation2, changes.NewProjectVersion.CompilationToAnalyze );
        Assert.Single( changes.SyntaxTreeChanges );

        var syntaxTreeChange = changes.SyntaxTreeChanges.Single().Value;

        Assert.Equal( SyntaxTreeChangeKind.Removed, syntaxTreeChange.SyntaxTreeChangeKind );
        Assert.Equal( CompileTimeChangeKind.None, syntaxTreeChange.CompileTimeChangeKind );
        Assert.True( syntaxTreeChange.NewSyntaxTreeVersion.IsDefault );
    }

    [Fact]
    public void ChangeSyntaxTree_ChangeDeclaration()
    {
        using var testContext = this.CreateTestContext();
        var code = new Dictionary<string, string> { { "code.cs", "class C {}" } };
        var compilation1 = testContext.CreateCSharpCompilation( code, assemblyName: nameof(this.ChangeSyntaxTree_ChangeDeclaration) );

        code["code.cs"] = "class D {}";

        var compilation2 = testContext.CreateCSharpCompilation( code, assemblyName: nameof(this.ChangeSyntaxTree_ChangeDeclaration) );
        var changes = this.CompareSyntaxTrees( compilation1, compilation2 );

        Assert.True( changes.HasChange );
        Assert.False( changes.HasCompileTimeCodeChange );

        Assert.True( changes.HasChange );
        Assert.True( changes.IsIncremental );
        Assert.Equal( compilation2, changes.NewProjectVersion.CompilationToAnalyze );
        Assert.Single( changes.SyntaxTreeChanges );

        var syntaxTreeChange = changes.SyntaxTreeChanges.Single().Value;

        Assert.Equal( SyntaxTreeChangeKind.Changed, syntaxTreeChange.SyntaxTreeChangeKind );
        Assert.Equal( CompileTimeChangeKind.None, syntaxTreeChange.CompileTimeChangeKind );
        Assert.Equal( compilation1.SyntaxTrees.Single(), syntaxTreeChange.OldSyntaxTreeVersionDangerous.SyntaxTree );
        Assert.Equal( compilation2.SyntaxTrees.Single(), syntaxTreeChange.NewSyntaxTreeVersion.SyntaxTree );
    }

    [Fact]
    public void ChangeSyntaxTree_ChangePartialTypeMembers()
    {
        using var testContext = this.CreateTestContext();
        var code = new Dictionary<string, string> { { "code.cs", "partial class C { void M() {} }" } };
        var compilation1 = testContext.CreateCSharpCompilation( code, assemblyName: nameof(this.ChangeSyntaxTree_ChangePartialTypeMembers) );

        code["code.cs"] = "partial class C { void N() {} }";

        var compilation2 = testContext.CreateCSharpCompilation( code, assemblyName: nameof(this.ChangeSyntaxTree_ChangePartialTypeMembers) );

        var changes = this.CompareSyntaxTrees( compilation1, compilation2 );

        Assert.False( changes.HasCompileTimeCodeChange );
        Assert.True( changes.HasChange );
        Assert.Single( changes.SyntaxTreeChanges );

        Assert.True( changes.IsIncremental );
        Assert.Equal( compilation2, changes.NewProjectVersion.CompilationToAnalyze );

        var syntaxTreeChange = changes.SyntaxTreeChanges.Single().Value;

        Assert.Empty( syntaxTreeChange.PartialTypeChanges );
        Assert.Equal( SyntaxTreeChangeKind.Changed, syntaxTreeChange.SyntaxTreeChangeKind );
        Assert.Equal( CompileTimeChangeKind.None, syntaxTreeChange.CompileTimeChangeKind );
        Assert.Equal( compilation1.SyntaxTrees.Single(), syntaxTreeChange.OldSyntaxTreeVersionDangerous.SyntaxTree );
        Assert.Equal( compilation2.SyntaxTrees.Single(), syntaxTreeChange.NewSyntaxTreeVersion.SyntaxTree );
    }

    [Fact]
    public void ChangeSyntaxTree_ChangeBody()
    {
        using var testContext = this.CreateTestContext();
        var code = new Dictionary<string, string> { { "code.cs", "class C { int M => 1; }" } };
        var compilation1 = testContext.CreateCSharpCompilation( code, assemblyName: nameof(this.ChangeSyntaxTree_ChangeBody) );

        code["code.cs"] = "class C { int M => 2; }";

        var compilation2 = testContext.CreateCSharpCompilation( code, assemblyName: nameof(this.ChangeSyntaxTree_ChangeBody) );

        var changes = this.CompareSyntaxTrees( compilation1, compilation2 );

        Assert.False( changes.HasChange );
        Assert.False( changes.HasCompileTimeCodeChange );
    }

    [Fact]
    public void ChangeSyntaxTree_AddPartialType()
    {
        using var testContext = this.CreateTestContext();
        var code = new Dictionary<string, string> { { "code.cs", "class C {}" } };
        var compilation1 = testContext.CreateCSharpCompilation( code, assemblyName: nameof(this.ChangeSyntaxTree_AddPartialType) );

        code["code.cs"] = "class C {} partial class D {}";

        var compilation2 = testContext.CreateCSharpCompilation( code, assemblyName: nameof(this.ChangeSyntaxTree_AddPartialType) );

        var changes = this.CompareSyntaxTrees( compilation1, compilation2 );

        Assert.False( changes.HasCompileTimeCodeChange );
        Assert.True( changes.HasChange );
        Assert.True( changes.IsIncremental );
        Assert.Single( changes.SyntaxTreeChanges );

        var syntaxTreeChange = changes.SyntaxTreeChanges.Single().Value;
        Assert.Single( syntaxTreeChange.PartialTypeChanges );
    }

    [Fact]
    public void ChangeSyntaxTree_NewlyCompileTime()
    {
        using var testContext = this.CreateTestContext();
        var code = new Dictionary<string, string> { { "code.cs", "class C {}" } };
        var compilation1 = testContext.CreateCSharpCompilation( code );

        code["code.cs"] = "using Metalama.Framework.Aspects;  class C {}";

        var compilation2 = testContext.CreateCSharpCompilation( code );

        var changes = this.CompareSyntaxTrees( compilation1, compilation2 );

        Assert.True( changes.HasCompileTimeCodeChange );
        Assert.True( changes.HasChange );
        Assert.True( changes.IsIncremental );
        Assert.Single( changes.SyntaxTreeChanges );
        Assert.Equal( CompileTimeChangeKind.NewlyCompileTime, changes.SyntaxTreeChanges.Single().Value.CompileTimeChangeKind );
    }

    [Fact]
    public void ChangeSyntaxTree_NoLongerCompileTime()
    {
        using var testContext = this.CreateTestContext();
        var code = new Dictionary<string, string> { { "code.cs", "using Metalama.Framework.Aspects;  class C {}" } };
        var compilation1 = testContext.CreateCSharpCompilation( code );

        code["code.cs"] = "class C {}";

        var compilation2 = testContext.CreateCSharpCompilation( code );

        var changes = this.CompareSyntaxTrees( compilation1, compilation2 );

        Assert.True( changes.HasCompileTimeCodeChange );
        Assert.True( changes.HasChange );
        Assert.True( changes.IsIncremental );
        Assert.Single( changes.SyntaxTreeChanges );
        Assert.Equal( CompileTimeChangeKind.NoLongerCompileTime, changes.SyntaxTreeChanges.Single().Value.CompileTimeChangeKind );
    }

    [Fact]
    public void DuplicateTrees()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateEmptyCSharpCompilation( null )
            .AddSyntaxTrees( SyntaxFactory.ParseSyntaxTree( "class C;", path: "C.cs" ), SyntaxFactory.ParseSyntaxTree( "internal class C;", path: "C.cs" ) );

        var changes = this.CompareSyntaxTrees( compilation, compilation );

        Assert.False( changes.HasChange );
    }
}