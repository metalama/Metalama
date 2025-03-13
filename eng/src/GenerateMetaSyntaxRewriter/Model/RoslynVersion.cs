// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.GenerateMetaSyntaxRewriter.Model;

internal sealed record RoslynVersion( string Name, int Index ) : IComparable<RoslynVersion>
{
    public string Name { get; } = Name;

    public int Index { get; } = Index;

    public int CompareTo( RoslynVersion? other ) => this.Index.CompareTo( other?.Index );

    public override string ToString() => this.Name;

    public string QualifiedEnumValue { get; } = $"RoslynApiVersion.V{Name.Replace( '.', '_' )}";

    public string EnumValue { get; } = $"V{Name.Replace( '.', '_' )}";
}