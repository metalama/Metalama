// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Linking;

internal static class LinkerGeneratedAnnotationExtensions
{
    private const string _annotationKind = "MetalamaAspectLinkerGeneratedNode";

    public static LinkerGeneratedFlags GetLinkerGeneratedFlags( this SyntaxNode node )
    {
        var annotations = node.GetAnnotations( _annotationKind );

        LinkerGeneratedFlags flags = default;

        foreach ( var annotation in annotations )
        {
            if ( annotation.Data != null )
            {
                flags |= LinkerGeneratedAnnotation.FromString( annotation.Data ).Flags;
            }
        }

        return flags;
    }

    public static LinkerGeneratedFlags GetLinkerGeneratedFlags( this SyntaxTrivia trivia )
    {
        var annotations = trivia.GetAnnotations( _annotationKind );

        LinkerGeneratedFlags flags = default;

        foreach ( var annotation in annotations )
        {
            if ( annotation.Data != null )
            {
                flags |= LinkerGeneratedAnnotation.FromString( annotation.Data ).Flags;
            }
        }

        return flags;
    }

    public static T WithLinkerGeneratedFlags<T>( this T node, in LinkerGeneratedFlags flags )
        where T : SyntaxNode
        => node.WithAdditionalAnnotations( new SyntaxAnnotation( _annotationKind, new LinkerGeneratedAnnotation( flags ).ToString() ) );
}