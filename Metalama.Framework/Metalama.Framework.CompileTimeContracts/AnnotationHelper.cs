// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.CompileTimeContracts;

[PublicAPI]
public static class AnnotationHelper
{
    private static readonly SyntaxAnnotation _flattenBlockAnnotation = new( "Metalama_Flatten" );

    public static BlockSyntax WithFlattenBlockAnnotation( this BlockSyntax block ) => block.WithAdditionalAnnotations( _flattenBlockAnnotation );

    public static bool HasFlattenBlockAnnotation( this BlockSyntax block ) => block.HasAnnotation( _flattenBlockAnnotation );
}