// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Metalama.Framework.Engine.Linking;

internal static class LinkerSyntaxHelper
{
    public static bool IsUnsupportedMemberSyntax( MemberDeclarationSyntax syntax )
    {
        return syntax switch
        {
            PropertyDeclarationSyntax { AccessorList.Accessors: { } accessors }
                when accessors.Any( a => a.IsKind( SyntaxKind.UnknownAccessorDeclaration ) ) => true,
            IndexerDeclarationSyntax { AccessorList.Accessors: { } accessors }
                when accessors.Any( a => a.IsKind( SyntaxKind.UnknownAccessorDeclaration ) ) => true,
            _ => false,
        };
    }
}