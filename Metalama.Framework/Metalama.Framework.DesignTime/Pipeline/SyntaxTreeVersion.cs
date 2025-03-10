// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Pipeline.Dependencies;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Pipeline;

public readonly record struct SyntaxTreeVersion(
    SyntaxTree SyntaxTree,
    bool HasCompileTimeCode,
    ulong DeclarationHash,
    ImmutableArray<TypeDependencyKey> PartialTypes = default,
    int PartialTypesHash = 0 )
{
    private readonly SyntaxTree? _syntaxTree = SyntaxTree;

    public SyntaxTree SyntaxTree => this._syntaxTree ?? throw new ArgumentNullException();

    internal bool IsDefault => this._syntaxTree == null;

    internal bool HasGlobalAttributes => this._syntaxTree?.ContainsGlobalAttributes() == true;

    internal SyntaxTreeVersion( SyntaxTree syntaxTree, in SyntaxTreeVersionData data ) : this(
        syntaxTree,
        data.HasCompileTimeCode,
        data.DeclarationHash,
        data.PartialTypes,
        data.PartialTypesHash )
    {
        Invariant.Assert( syntaxTree.ContainsGlobalAttributes() == data.HasGlobalAttributes );
    }
}

internal readonly record struct SyntaxTreeVersionData(
    bool HasCompileTimeCode,
    bool HasGlobalAttributes,
    ulong DeclarationHash,
    ImmutableArray<TypeDependencyKey> PartialTypes = default,
    int PartialTypesHash = 0 )
{
    public SyntaxTreeVersionData( in SyntaxTreeVersion version ) : this(
        version.HasCompileTimeCode,
        version.HasGlobalAttributes,
        version.DeclarationHash,
        version.PartialTypes,
        version.PartialTypesHash ) { }
}