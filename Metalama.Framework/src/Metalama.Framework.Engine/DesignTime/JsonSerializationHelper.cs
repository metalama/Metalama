// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Formatting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using System.Threading;

namespace Metalama.Framework.Engine.DesignTime;

public static partial class JsonSerializationHelper
{
    private static SyntaxAnnotation ToSyntaxAnnotation( this SerializableAnnotation serializableAnnotation )
        => serializableAnnotation.Kind switch
        {
            SerializableAnnotationKind.Formatter => Formatter.Annotation,
            SerializableAnnotationKind.Simplifier => Simplifier.Annotation,
            SerializableAnnotationKind.GeneratedCode => new SyntaxAnnotation( FormattingAnnotations.GeneratedCodeAnnotationKind, serializableAnnotation.Data ),
            SerializableAnnotationKind.SourceCode => FormattingAnnotations.SourceCodeAnnotation,
            _ => throw new AssertionFailedException( $"Unexpected kind: {serializableAnnotation.Kind}." )
        };

    public static SerializableSyntaxTree CreateSerializableSyntaxTree( SyntaxTree syntaxTree )
        => CreateSerializableSyntaxTree( syntaxTree.GetRoot(), syntaxTree.FilePath );

    public static SerializableSyntaxTree CreateSerializableSyntaxTree( SyntaxNode syntaxRoot, string filePath )
    {
        var annotationReader = new AnnotationReader();
        annotationReader.Visit( syntaxRoot );

        return new SerializableSyntaxTree( filePath, syntaxRoot.GetText().ToString(), annotationReader.GetAnnotations() );
    }

    public static SyntaxTree ToSyntaxTree(
        this SerializableSyntaxTree serializableSyntaxTree,
        CSharpParseOptions options,
        CancellationToken cancellationToken = default )
    {
        var unannotatedTree = CSharpSyntaxTree.ParseText( serializableSyntaxTree.Text, options, path: serializableSyntaxTree.FilePath );
        var rewriter = new AnnotationWriter( serializableSyntaxTree, cancellationToken );

        return unannotatedTree.WithRootAndOptions( rewriter.Visit( unannotatedTree.GetRoot() )!, options );
    }

    internal static SyntaxNode ToSyntaxNode( this SerializableSyntaxTree serializableSyntaxTree, CancellationToken cancellationToken = default )
    {
        var unannotatedTree = CSharpSyntaxTree.ParseText( serializableSyntaxTree.Text, CSharpParseOptions.Default );
        var rewriter = new AnnotationWriter( serializableSyntaxTree, cancellationToken );

        return rewriter.Visit( unannotatedTree.GetRoot() )!;
    }
}