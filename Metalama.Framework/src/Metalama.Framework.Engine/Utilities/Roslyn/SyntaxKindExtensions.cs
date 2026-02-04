// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

/// <summary>
/// Extension properties for <see cref="SyntaxKind"/>.
/// </summary>
public static class SyntaxKindExtensions
{
    extension( SyntaxNode node )
    {
        public SyntaxKind SyntaxKind => node.Kind();
    }

    extension( SyntaxToken token )
    {
        public SyntaxKind SyntaxKind => token.Kind();
    }

    extension( SyntaxKind kind )
    {
        /// <summary>
        /// Gets a value indicating whether the syntax kind represents a type declaration that can contain members
        /// (class, struct, interface, record, or record struct).
        /// </summary>
        public bool IsTypeDeclaration
            => kind is SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration or SyntaxKind.InterfaceDeclaration
                or SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration;

        /// <summary>
        /// Gets a value indicating whether the syntax kind represents any base type declaration
        /// (class, struct, interface, record, record struct, enum, or delegate).
        /// </summary>
        public bool IsBaseTypeDeclaration => kind.IsTypeDeclaration || kind is SyntaxKind.EnumDeclaration or SyntaxKind.DelegateDeclaration;

        /// <summary>
        /// Gets a value indicating whether the syntax kind represents a lambda expression
        /// (simple lambda or parenthesized lambda).
        /// </summary>
        public bool IsLambdaExpression => kind is SyntaxKind.SimpleLambdaExpression or SyntaxKind.ParenthesizedLambdaExpression;

        /// <summary>
        /// Gets a value indicating whether the syntax kind represents a base field declaration
        /// (field or event field).
        /// </summary>
        public bool IsBaseFieldDeclaration => kind is SyntaxKind.FieldDeclaration or SyntaxKind.EventFieldDeclaration;

        /// <summary>
        /// Gets a value indicating whether the syntax kind represents a literal expression
        /// (character, string, numeric, true, false, null, or default literal).
        /// </summary>
        public bool IsLiteralExpression
            => kind is SyntaxKind.CharacterLiteralExpression or SyntaxKind.StringLiteralExpression
                or SyntaxKind.NumericLiteralExpression or SyntaxKind.TrueLiteralExpression
                or SyntaxKind.FalseLiteralExpression or SyntaxKind.NullLiteralExpression
                or SyntaxKind.DefaultLiteralExpression;

        /// <summary>
        /// Gets a value indicating whether the syntax kind represents an accessor declaration
        /// (get, set, init, add, or remove accessor).
        /// </summary>
        public bool IsAccessorDeclaration
            => kind is SyntaxKind.GetAccessorDeclaration or SyntaxKind.SetAccessorDeclaration
                or SyntaxKind.InitAccessorDeclaration or SyntaxKind.AddAccessorDeclaration
                or SyntaxKind.RemoveAccessorDeclaration;

        /// <summary>
        /// Gets a value indicating whether the syntax kind represents a base method declaration
        /// (method, constructor, destructor, operator, or conversion operator).
        /// </summary>
        public bool IsBaseMethodDeclaration
            => kind is SyntaxKind.MethodDeclaration or SyntaxKind.ConstructorDeclaration
                or SyntaxKind.DestructorDeclaration or SyntaxKind.OperatorDeclaration
                or SyntaxKind.ConversionOperatorDeclaration;

        /// <summary>
        /// Gets a value indicating whether the syntax kind represents a property, event, or indexer declaration.
        /// </summary>
        public bool IsPropertyOrEventDeclaration
            => kind is SyntaxKind.PropertyDeclaration or SyntaxKind.EventDeclaration
                or SyntaxKind.IndexerDeclaration;

        /// <summary>
        /// Gets a value indicating whether the syntax kind represents a simple name
        /// (identifier name or generic name).
        /// </summary>
        public bool IsSimpleName => kind is SyntaxKind.IdentifierName or SyntaxKind.GenericName;

        /// <summary>
        /// Gets a value indicating whether the syntax kind represents a record declaration
        /// (record or record struct).
        /// </summary>
        public bool IsRecordDeclaration => kind is SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration;

        /// <summary>
        /// Gets a value indicating whether the syntax kind represents a name
        /// (simple name, qualified name, or alias qualified name).
        /// </summary>
        public bool IsName
            => kind is SyntaxKind.IdentifierName or SyntaxKind.GenericName
                or SyntaxKind.QualifiedName or SyntaxKind.AliasQualifiedName;

        /// <summary>
        /// Gets a value indicating whether the syntax kind represents a namespace declaration
        /// (namespace or file-scoped namespace).
        /// </summary>
        public bool IsNamespaceDeclaration
            => kind is SyntaxKind.NamespaceDeclaration or SyntaxKind.FileScopedNamespaceDeclaration;
    }
}