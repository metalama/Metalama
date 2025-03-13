// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code.Comparers;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.CodeModel.Comparers;

// ReSharper disable once IdentifierTypo
internal sealed class CompilationComparers : ICompilationComparers
{
    // ReSharper disable once IdentifierTypo
    public CompilationComparers( Compilation compilation )
    {
        this.Default = new DeclarationEqualityComparer( compilation, false );
        this.IncludeNullability = new DeclarationEqualityComparer( compilation, true );
    }

    public IDeclarationComparer Default { get; }

    public ITypeComparer IncludeNullability { get; }

    public ITypeComparer GetTypeComparer( TypeComparison comparison )
        => comparison switch
        {
            TypeComparison.Default => this.Default,
            TypeComparison.IncludeNullability => this.IncludeNullability,
            _ => throw new ArgumentOutOfRangeException()
        };
}