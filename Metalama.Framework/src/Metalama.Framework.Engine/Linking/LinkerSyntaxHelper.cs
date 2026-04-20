// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Metalama.Framework.Engine.Linking;

internal static class LinkerSyntaxHelper
{
    public static bool IsUnsupportedMemberSyntax( MemberDeclarationSyntax syntax )
    {
        return syntax.Kind() switch
        {
            SyntaxKind.PropertyDeclaration when syntax is PropertyDeclarationSyntax { AccessorList.Accessors: { } accessors }
                                                && accessors.Any( a => a.IsKind( SyntaxKind.UnknownAccessorDeclaration ) ) => true,
            SyntaxKind.IndexerDeclaration when syntax is IndexerDeclarationSyntax { AccessorList.Accessors: { } accessors }
                                               && accessors.Any( a => a.IsKind( SyntaxKind.UnknownAccessorDeclaration ) ) => true,
            _ => false
        };
    }
}