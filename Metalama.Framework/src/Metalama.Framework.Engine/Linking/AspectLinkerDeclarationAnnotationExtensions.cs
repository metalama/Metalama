// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Metalama.Framework.Engine.Linking;

/// <exclude />
[UsedImplicitly]
public static class AspectLinkerDeclarationAnnotationExtensions
{
    [UsedImplicitly]
    public const string DeclarationAnnotationKind = "MetalamaAspectLinkerDeclarationNode";

    internal static AspectLinkerDeclarationFlags GetLinkerDeclarationFlags( this SyntaxNode node )
    {
        var annotationValue = node.GetAnnotations( DeclarationAnnotationKind ).SingleOrDefault()?.Data;

        return annotationValue != null ? AspectLinkerDeclarationAnnotation.FromString( annotationValue ).Flags : AspectLinkerDeclarationFlags.None;
    }

    internal static T WithLinkerDeclarationFlags<T>( this T node, in AspectLinkerDeclarationFlags flags )
        where T : MemberDeclarationSyntax
    {
        var existingAnnotation = node.GetAnnotations( DeclarationAnnotationKind ).SingleOrDefault();

        if ( existingAnnotation != null )
        {
            node = node.WithoutAnnotations( existingAnnotation );
        }

        return node.WithAdditionalAnnotations( new SyntaxAnnotation( DeclarationAnnotationKind, new AspectLinkerDeclarationAnnotation( flags ).ToString() ) );
    }
}