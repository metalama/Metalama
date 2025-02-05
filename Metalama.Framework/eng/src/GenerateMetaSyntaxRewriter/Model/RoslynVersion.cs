// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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