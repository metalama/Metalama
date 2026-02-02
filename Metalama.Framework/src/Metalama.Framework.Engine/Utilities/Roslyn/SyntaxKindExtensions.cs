// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis.CSharp;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

/// <summary>
/// Extension methods for <see cref="SyntaxKind"/>.
/// </summary>
internal static class SyntaxKindExtensions
{
    /// <summary>
    /// Determines whether the syntax kind represents a type declaration that can contain members
    /// (class, struct, interface, record, or record struct).
    /// </summary>
    public static bool IsTypeDeclaration( this SyntaxKind kind )
        => kind is SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration or SyntaxKind.InterfaceDeclaration
            or SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration;

    /// <summary>
    /// Determines whether the syntax kind represents any base type declaration
    /// (class, struct, interface, record, record struct, enum, or delegate).
    /// </summary>
    public static bool IsBaseTypeDeclaration( this SyntaxKind kind )
        => kind.IsTypeDeclaration() || kind is SyntaxKind.EnumDeclaration or SyntaxKind.DelegateDeclaration;

    /// <summary>
    /// Determines whether the syntax kind represents a lambda expression
    /// (simple lambda or parenthesized lambda).
    /// </summary>
    public static bool IsLambdaExpression( this SyntaxKind kind )
        => kind is SyntaxKind.SimpleLambdaExpression or SyntaxKind.ParenthesizedLambdaExpression;

    /// <summary>
    /// Determines whether the syntax kind represents a base field declaration
    /// (field or event field).
    /// </summary>
    public static bool IsBaseFieldDeclaration( this SyntaxKind kind )
        => kind is SyntaxKind.FieldDeclaration or SyntaxKind.EventFieldDeclaration;

    /// <summary>
    /// Determines whether the syntax kind represents a literal expression
    /// (character, string, numeric, true, false, null, or default literal).
    /// </summary>
    public static bool IsLiteralExpression( this SyntaxKind kind )
        => kind is SyntaxKind.CharacterLiteralExpression or SyntaxKind.StringLiteralExpression
            or SyntaxKind.NumericLiteralExpression or SyntaxKind.TrueLiteralExpression
            or SyntaxKind.FalseLiteralExpression or SyntaxKind.NullLiteralExpression
            or SyntaxKind.DefaultLiteralExpression;
}
