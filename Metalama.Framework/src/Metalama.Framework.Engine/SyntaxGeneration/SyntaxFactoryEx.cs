// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Formatting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;
using System;
using System.Collections.Concurrent;
using System.Linq;
using RefKind = Metalama.Framework.Code.RefKind;

namespace Metalama.Framework.Engine.SyntaxGeneration;

/// <summary>
/// Helper methods that would ideally be in the <see cref="SyntaxFactory"/> class.
/// </summary>
public static partial class SyntaxFactoryEx
{
    private static readonly ConcurrentDictionary<SyntaxKind, SyntaxToken> _tokensWithTrailingSpace = new();

    internal static LiteralExpressionSyntax Null => SyntaxFactory.LiteralExpression( SyntaxKind.NullLiteralExpression );

    internal static LiteralExpressionSyntax Default
        => SyntaxFactory.LiteralExpression(
            SyntaxKind.DefaultLiteralExpression,
            SyntaxFactory.Token( SyntaxKind.DefaultKeyword ) );

    internal static SyntaxTriviaList ElasticSpaceTriviaList { get; } = new( SyntaxFactory.ElasticSpace );

    internal static SyntaxTriviaList ElasticLineFeedTriviaList { get; } = new( SyntaxFactory.ElasticLineFeed );

    public static SyntaxToken TokenWithTrailingSpace( SyntaxKind kind )
        => _tokensWithTrailingSpace.GetOrAdd( kind, static k => SyntaxFactory.Token( default, k, ElasticSpaceTriviaList ) );

    internal static SyntaxToken InvocationRefKindToken( this RefKind refKind )
        => refKind switch
        {
            RefKind.None or RefKind.In => default,
            RefKind.Out => SyntaxFactory.Token( SyntaxKind.OutKeyword ),
            RefKind.Ref => SyntaxFactory.Token( SyntaxKind.RefKeyword ),
            RefKind.RefReadOnly => SyntaxFactory.Token( SyntaxKind.InKeyword ),
            _ => throw new AssertionFailedException( $"Unexpected RefKind: {refKind}." )
        };

    internal static ExpressionStatementSyntax DiscardStatement( ExpressionSyntax discardedExpression )
        => SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression( SyntaxKind.SimpleAssignmentExpression, DiscardIdentifierName(), discardedExpression ) );

#pragma warning disable LAMA0850 // Intentional use of SyntaxFactory.Identifier/IdentifierName for special tokens
    internal static IdentifierNameSyntax DiscardIdentifierName() => SyntaxFactory.IdentifierName( DiscardIdentifier() );

    public static SyntaxToken DiscardIdentifier()
    {
        return SyntaxFactory.Identifier(
            SyntaxFactory.TriviaList(),
            SyntaxKind.UnderscoreToken,
            "_",
            "_",
            SyntaxFactory.TriviaList() );
    }

    internal static IdentifierNameSyntax VarIdentifier()
        => SyntaxFactory.IdentifierName(
            SyntaxFactory.Identifier(
                SyntaxFactory.TriviaList(),
                SyntaxKind.VarKeyword,
                "var",
                "var",
                SyntaxFactory.TriviaList( SyntaxFactory.ElasticSpace ) ) );
#pragma warning restore LAMA0850

    /// <summary>
    /// Creates a safe identifier token from a name, escaping it with @ prefix if it's a C# keyword.
    /// If the name already starts with @, it is returned as-is to prevent double escaping.
    /// </summary>
    /// <param name="name">The identifier name.</param>
#pragma warning disable LAMA0850 // Intentional use of SyntaxFactory.Identifier
    internal static SyntaxToken SafeIdentifier( string name )
    {
        // If the name already starts with @, return it as-is to prevent double escaping (@@).
        if ( name.StartsWith( "@", StringComparison.Ordinal ) )
        {
            return SyntaxFactory.Identifier( name );
        }

        var keywordKind = SyntaxFacts.GetKeywordKind( name );

        if ( keywordKind != SyntaxKind.None )
        {
            // The name is a C# keyword, so we need to escape it with @
            return SyntaxFactory.Identifier(
                SyntaxFactory.TriviaList(),
                keywordKind,
                "@" + name,
                name,
                SyntaxFactory.TriviaList() );
        }

        return SyntaxFactory.Identifier( name );
    }

    /// <summary>
    /// Creates a safe identifier token from a name, escaping it with @ prefix if it's a C# keyword.
    /// Preserves the specified leading and trailing trivia.
    /// </summary>
    /// <param name="leading">Leading trivia.</param>
    /// <param name="name">The identifier name.</param>
    /// <param name="trailing">Trailing trivia.</param>
    internal static SyntaxToken SafeIdentifier( SyntaxTriviaList leading, string name, SyntaxTriviaList trailing )
    {
        // If the name already starts with @, return it as-is to prevent double escaping (@@).
        if ( name.StartsWith( "@", StringComparison.Ordinal ) )
        {
            return SyntaxFactory.Identifier( leading, name, trailing );
        }

        var keywordKind = SyntaxFacts.GetKeywordKind( name );

        if ( keywordKind != SyntaxKind.None )
        {
            // The name is a C# keyword, so we need to escape it with @
            return SyntaxFactory.Identifier(
                leading,
                keywordKind,
                "@" + name,
                name,
                trailing );
        }

        return SyntaxFactory.Identifier( leading, name, trailing );
    }

    /// <summary>
    /// Creates an identifier token for a well-known name that is guaranteed not to be a C# keyword.
    /// Use this for generated names with prefixes (e.g., "__aspectInstance"), type names, or namespace parts.
    /// </summary>
    /// <param name="name">The well-known identifier name.</param>
    internal static SyntaxToken WellKnownIdentifier( string name ) => SyntaxFactory.Identifier( name );

    /// <summary>
    /// Creates an identifier token for a well-known name that is guaranteed not to be a C# keyword.
    /// Preserves the specified leading and trailing trivia.
    /// </summary>
    /// <param name="leading">Leading trivia.</param>
    /// <param name="name">The well-known identifier name.</param>
    /// <param name="trailing">Trailing trivia.</param>
    internal static SyntaxToken WellKnownIdentifier( SyntaxTriviaList leading, string name, SyntaxTriviaList trailing )
        => SyntaxFactory.Identifier( leading, name, trailing );
#pragma warning restore LAMA0850

    /// <summary>
    /// Creates a safe identifier name syntax from a name, escaping it with @ prefix if it's a C# keyword.
    /// </summary>
    /// <param name="name">The identifier name.</param>
#pragma warning disable LAMA0850 // False positive: passing SyntaxToken from SafeIdentifier, not a string
    internal static IdentifierNameSyntax SafeIdentifierName( string name )
        => SyntaxFactory.IdentifierName( SafeIdentifier( name ) );
#pragma warning restore LAMA0850

    /// <summary>
    /// Creates an identifier name syntax for a well-known name that is guaranteed not to be a C# keyword.
    /// Use this for generated names with prefixes (e.g., "__aspectInstance"), type names, or namespace parts.
    /// </summary>
    /// <param name="name">The well-known identifier name.</param>
#pragma warning disable LAMA0850 // False positive: passing SyntaxToken from WellKnownIdentifier, not a string
    internal static IdentifierNameSyntax WellKnownIdentifierName( string name )
        => SyntaxFactory.IdentifierName( WellKnownIdentifier( name ) );
#pragma warning restore LAMA0850

    /// <summary>
    /// Creates an identifier name syntax for a well-known name that is guaranteed not to be a C# keyword.
    /// Preserves the specified leading and trailing trivia.
    /// </summary>
    /// <param name="leading">Leading trivia.</param>
    /// <param name="name">The well-known identifier name.</param>
    /// <param name="trailing">Trailing trivia.</param>
#pragma warning disable LAMA0850 // False positive: passing SyntaxToken from WellKnownIdentifier, not a string
    internal static IdentifierNameSyntax WellKnownIdentifierName( SyntaxTriviaList leading, string name, SyntaxTriviaList trailing )
        => SyntaxFactory.IdentifierName( WellKnownIdentifier( leading, name, trailing ) );
#pragma warning restore LAMA0850

    /// <summary>
    /// Creates an identifier name syntax from a well-known token (e.g., GlobalKeyword).
    /// </summary>
    /// <param name="token">The well-known identifier token.</param>
#pragma warning disable LAMA0850 // Intentional use for well-known tokens
    internal static IdentifierNameSyntax WellKnownIdentifierName( SyntaxToken token )
        => SyntaxFactory.IdentifierName( token );
#pragma warning restore LAMA0850

    [PublicAPI]
    public static LiteralExpressionSyntax LiteralNonNullExpression( string s )
        => SyntaxFactory.LiteralExpression( SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal( s ) );

    [PublicAPI]
    public static LiteralExpressionSyntax LiteralExpression( int i, ObjectDisplayOptions options = ObjectDisplayOptions.None )
        => SyntaxFactory.LiteralExpression( SyntaxKind.NumericLiteralExpression, LiteralImpl( i, options ) );

    [PublicAPI]
    public static LiteralExpressionSyntax LiteralExpression( uint i, ObjectDisplayOptions options = ObjectDisplayOptions.None )
        => SyntaxFactory.LiteralExpression( SyntaxKind.NumericLiteralExpression, LiteralImpl( i, options ) );

    [PublicAPI]
    public static LiteralExpressionSyntax LiteralExpression( short i, ObjectDisplayOptions options = ObjectDisplayOptions.None )
        => SyntaxFactory.LiteralExpression( SyntaxKind.NumericLiteralExpression, LiteralImpl( (int) i, options ) );

    [PublicAPI]
    public static LiteralExpressionSyntax LiteralExpression( ushort i, ObjectDisplayOptions options = ObjectDisplayOptions.None )
        => SyntaxFactory.LiteralExpression( SyntaxKind.NumericLiteralExpression, LiteralImpl( (uint) i, options ) );

    [PublicAPI]
    public static LiteralExpressionSyntax LiteralExpression( long i, ObjectDisplayOptions options = ObjectDisplayOptions.None )
        => SyntaxFactory.LiteralExpression( SyntaxKind.NumericLiteralExpression, LiteralImpl( i, options ) );

    [PublicAPI]
    public static LiteralExpressionSyntax LiteralExpression( ulong i, ObjectDisplayOptions options = ObjectDisplayOptions.None )
        => SyntaxFactory.LiteralExpression( SyntaxKind.NumericLiteralExpression, LiteralImpl( i, options ) );

    [PublicAPI]
    public static LiteralExpressionSyntax LiteralExpression( float i, ObjectDisplayOptions options = ObjectDisplayOptions.None )
        => SyntaxFactory.LiteralExpression( SyntaxKind.NumericLiteralExpression, LiteralImpl( i, options ) );

    [PublicAPI]
    public static LiteralExpressionSyntax LiteralExpression( double i, ObjectDisplayOptions options = ObjectDisplayOptions.None )
        => SyntaxFactory.LiteralExpression( SyntaxKind.NumericLiteralExpression, LiteralImpl( i, options ) );

    [PublicAPI]
    public static LiteralExpressionSyntax LiteralExpression( decimal i, ObjectDisplayOptions options = ObjectDisplayOptions.None )
        => SyntaxFactory.LiteralExpression( SyntaxKind.NumericLiteralExpression, LiteralImpl( i, options ) );

    [PublicAPI]
    public static LiteralExpressionSyntax LiteralExpression( char c )
        => SyntaxFactory.LiteralExpression( SyntaxKind.CharacterLiteralExpression, SyntaxFactory.Literal( c ) );

    [PublicAPI]
    public static LiteralExpressionSyntax LiteralExpression( bool b )
        => SyntaxFactory.LiteralExpression( b ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression );

    private static SyntaxToken LiteralImpl<T>( T value, ObjectDisplayOptions options = ObjectDisplayOptions.None )
        => LiteralFormatter<T>.Instance.Format( value, options );

    internal static TypeSyntax ExpressionSyntaxType { get; } = SyntaxFactory.QualifiedName(
        SyntaxFactory.QualifiedName(
            SyntaxFactory.QualifiedName(
                SyntaxFactory.QualifiedName(
                    SyntaxFactory.AliasQualifiedName(
                        WellKnownIdentifierName( SyntaxFactory.Token( SyntaxKind.GlobalKeyword ) ),
                        WellKnownIdentifierName( "Microsoft" ) ),
                    WellKnownIdentifierName( "CodeAnalysis" ) ),
                WellKnownIdentifierName( "CSharp" ) ),
            WellKnownIdentifierName( "Syntax" ) ),
        WellKnownIdentifierName( "ExpressionSyntax" ) );

    internal static LiteralExpressionSyntax LiteralExpression( object literal, ObjectDisplayOptions options = ObjectDisplayOptions.None )
    {
        return (LiteralExpressionSyntax?) LiteralExpressionOrNull( literal, options )
               ?? throw new ArgumentOutOfRangeException( nameof(literal), $"'{literal}' is not a valid literal." );
    }

    internal static ExpressionSyntax? LiteralExpressionOrNull( object? obj, ObjectDisplayOptions options = ObjectDisplayOptions.None )
        => obj switch
        {
            null => Null,
            byte b => LiteralExpression( (int) b, options ),
            sbyte b => LiteralExpression( (int) b, options ),
            string s => LiteralExpression( s ),
            char c => LiteralExpression( c ),
            int i => LiteralExpression( i, options ),
            uint i => LiteralExpression( i, options ),
            long l => LiteralExpression( l, options ),
            ulong l => LiteralExpression( l, options ),
            short s => LiteralExpression( (int) s, options ),
            ushort s => LiteralExpression( (int) s, options ),
            double d => LiteralExpression( d, options ),
            float f => LiteralExpression( f, options ),

            // force type suffix for decimal, since code like "decimal d = 3.14;" is not valid
            decimal d => LiteralExpression( d, options | ObjectDisplayOptions.IncludeTypeSuffix ),
            bool b => LiteralExpression( b ),
            _ => null
        };

    internal static ExpressionSyntax LiteralExpression( string? s )
        => s == null
            ? SyntaxFactory.ParenthesizedExpression(
                    SyntaxFactory.CastExpression(
                        SyntaxFactory.NullableType( SyntaxFactory.PredefinedType( SyntaxFactory.Token( SyntaxKind.StringKeyword ) ) ),
                        SyntaxFactory.LiteralExpression( SyntaxKind.NullLiteralExpression ) ) )
                .WithAdditionalAnnotations( Simplifier.Annotation )
            : LiteralNonNullExpression( s );

    internal static ExpressionSyntax ParseExpressionSafe( string text )
    {
        var expression = SyntaxFactory.ParseExpression( text );

        var diagnostics = expression.GetDiagnostics().ToArray();

        if ( diagnostics.HasError() )
        {
#pragma warning disable CA1307 // StringComparison overload not available on net472
            var sanitizedText = text.Replace( "\r\n", "\\n" ).Replace( "\r", "\\r" ).Replace( "\n", "\\n" );
#pragma warning restore CA1307

            throw new DiagnosticException( $"Code '{sanitizedText}' could not be parsed as an expression.", diagnostics.ToImmutableArray(), false );
        }

        return expression;
    }

    internal static StatementSyntax ParseStatementSafe( string text )
    {
        var statement = SyntaxFactory.ParseStatement( text );

        var diagnostics = statement.GetDiagnostics();
        var enumerable = diagnostics as Diagnostic[] ?? diagnostics.ToArray();

        if ( enumerable.HasError() )
        {
#pragma warning disable CA1307 // StringComparison overload not available on net472
            var sanitizedText = text.Replace( "\r\n", "\\n" ).Replace( "\r", "\\r" ).Replace( "\n", "\\n" );
#pragma warning restore CA1307

            throw new DiagnosticException( $"Code '{sanitizedText}' could not be parsed as a statement.", enumerable.ToImmutableArray(), false );
        }

        return statement;
    }

#pragma warning disable LAMA0850 // Intentional use for missing/empty tokens
    private static ExpressionSyntax EmptyExpression => SyntaxFactory.IdentifierName( SyntaxFactory.MissingToken( SyntaxKind.IdentifierToken ) );
#pragma warning restore LAMA0850

    internal static StatementSyntax EmptyStatement
        => SyntaxFactory.ExpressionStatement( EmptyExpression, SyntaxFactory.MissingToken( SyntaxKind.SemicolonToken ) );

    internal static ExpressionStatementSyntax AssignmentStatement(
        ExpressionSyntax left,
        ExpressionSyntax right,
        SyntaxGenerationContext syntaxGenerationContext )
        => SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    left,
                    SyntaxFactory.Token(
                        SyntaxFactory.TriviaList( SyntaxFactory.ElasticSpace ),
                        SyntaxKind.EqualsToken,
                        SyntaxFactory.TriviaList( SyntaxFactory.ElasticSpace ) ),
                    right ),
                SyntaxFactory.Token( default, SyntaxKind.SemicolonToken, syntaxGenerationContext.OptionalElasticEndOfLineTriviaList ) )
            .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation );
}