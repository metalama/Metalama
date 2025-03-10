// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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