// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.CodeModel.Helpers
{
    public static partial class DependencyAnalysisHelper
    {
        // ReSharper disable once UnusedMember.Global
        public static void FindDeclaredAndAttributeTypes(
            SemanticModel semanticModel,
            Action<INamedTypeSymbol> addDeclaredType,
            Action<INamedTypeSymbol> addAttributeType )
        {
            var visitor = new FindDeclaredAndAttributeTypesVisitor( semanticModel, addDeclaredType, addAttributeType );
            visitor.Visit( semanticModel.SyntaxTree.GetRoot() );
        }

        public static void FindDeclaredTypes( SemanticModel semanticModel, Action<INamedTypeSymbol> addDeclaredType )
        {
            var visitor = new FindDeclaredTypesVisitor( semanticModel, addDeclaredType );
            visitor.Visit( semanticModel.SyntaxTree.GetRoot() );
        }
    }
}