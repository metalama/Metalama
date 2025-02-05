// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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