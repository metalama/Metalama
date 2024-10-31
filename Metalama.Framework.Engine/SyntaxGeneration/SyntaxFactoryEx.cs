// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
internal static partial class SyntaxFactoryEx
{
    private static readonly ConcurrentDictionary<SyntaxKind, SyntaxToken> _tokensWithTrailingSpace = new();

    public static LiteralExpressionSyntax Null => SyntaxFactory.LiteralExpression( SyntaxKind.NullLiteralExpression );

    public static LiteralExpressionSyntax Default
        => SyntaxFactory.LiteralExpression(
            SyntaxKind.DefaultLiteralExpression,
            SyntaxFactory.Token( SyntaxKind.DefaultKeyword ) );

    internal static SyntaxToken TokenWithTrailingSpace( SyntaxKind kind )
        => _tokensWithTrailingSpace.GetOrAdd( kind, static k => SyntaxFactory.Token( default, k, new SyntaxTriviaList( SyntaxFactory.ElasticSpace ) ) );

    internal static SyntaxToken InvocationRefKindToken( this RefKind refKind )
        => refKind switch
        {
            RefKind.None or RefKind.In => default,
            RefKind.Out => SyntaxFactory.Token( SyntaxKind.OutKeyword ),
            RefKind.Ref => SyntaxFactory.Token( SyntaxKind.RefKeyword ),
            RefKind.RefReadOnly => SyntaxFactory.Token( SyntaxKind.InKeyword ),
            _ => throw new AssertionFailedException( $"Unexpected RefKind: {refKind}." )
        };

    public static ExpressionStatementSyntax DiscardStatement( ExpressionSyntax discardedExpression )
        => SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression( SyntaxKind.SimpleAssignmentExpression, DiscardIdentifier(), discardedExpression ) );

    public static IdentifierNameSyntax DiscardIdentifier()
        => SyntaxFactory.IdentifierName(
            SyntaxFactory.Identifier(
                SyntaxFactory.TriviaList(),
                SyntaxKind.UnderscoreToken,
                "_",
                "_",
                SyntaxFactory.TriviaList() ) );

    public static IdentifierNameSyntax VarIdentifier()
        => SyntaxFactory.IdentifierName(
            SyntaxFactory.Identifier(
                SyntaxFactory.TriviaList(),
                SyntaxKind.VarKeyword,
                "var",
                "var",
                SyntaxFactory.TriviaList( SyntaxFactory.ElasticSpace ) ) );

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
    public static LiteralExpressionSyntax LiteralExpression( char c, ObjectDisplayOptions options = ObjectDisplayOptions.None )
        => SyntaxFactory.LiteralExpression( SyntaxKind.CharacterLiteralExpression, LiteralImpl( c, options ) );

    [PublicAPI]
    public static LiteralExpressionSyntax LiteralExpression( bool b )
        => SyntaxFactory.LiteralExpression( b ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression );

    private static SyntaxToken LiteralImpl<T>( T value, ObjectDisplayOptions options = ObjectDisplayOptions.None )
        => LiteralFormatter<T>.Instance.Format( value, options );

    public static TypeSyntax ExpressionSyntaxType { get; } = SyntaxFactory.QualifiedName(
        SyntaxFactory.QualifiedName(
            SyntaxFactory.QualifiedName(
                SyntaxFactory.QualifiedName(
                    SyntaxFactory.AliasQualifiedName(
                        SyntaxFactory.IdentifierName( SyntaxFactory.Token( SyntaxKind.GlobalKeyword ) ),
                        SyntaxFactory.IdentifierName( "Microsoft" ) ),
                    SyntaxFactory.IdentifierName( "CodeAnalysis" ) ),
                SyntaxFactory.IdentifierName( "CSharp" ) ),
            SyntaxFactory.IdentifierName( "Syntax" ) ),
        SyntaxFactory.IdentifierName( "ExpressionSyntax" ) );

    public static LiteralExpressionSyntax LiteralExpression( object literal, ObjectDisplayOptions options = ObjectDisplayOptions.None )
    {
        return (LiteralExpressionSyntax?) LiteralExpressionOrNull( literal, options )
               ?? throw new ArgumentOutOfRangeException( nameof(literal), $"'{literal}' is not a valid literal." );
    }

    public static ExpressionSyntax? LiteralExpressionOrNull( object? obj, ObjectDisplayOptions options = ObjectDisplayOptions.None )
        => obj switch
        {
            null => Null,
            byte b => LiteralExpression( (int) b, options ),
            sbyte b => LiteralExpression( (int) b, options ),
            string s => LiteralExpression( s ),
            char s => LiteralExpression( s, options ),
            int s => LiteralExpression( s, options ),
            uint s => LiteralExpression( s, options ),
            long s => LiteralExpression( s, options ),
            ulong s => LiteralExpression( s, options ),
            short s => LiteralExpression( (int) s, options ),
            ushort s => LiteralExpression( (int) s, options ),
            double s => LiteralExpression( s, options ),
            float s => LiteralExpression( s, options ),

            // force type suffix for decimal, since code like "decimal d = 3.14;" is not valid
            decimal s => LiteralExpression( s, options | ObjectDisplayOptions.IncludeTypeSuffix ),
            bool b => LiteralExpression( b ),
            _ => null
        };

    public static ExpressionSyntax LiteralExpression( string? s )
        => s == null
            ? SyntaxFactory.ParenthesizedExpression(
                    SyntaxFactory.CastExpression(
                        SyntaxFactory.NullableType( SyntaxFactory.PredefinedType( SyntaxFactory.Token( SyntaxKind.StringKeyword ) ) ),
                        SyntaxFactory.LiteralExpression( SyntaxKind.NullLiteralExpression ) ) )
                .WithAdditionalAnnotations( Simplifier.Annotation )
            : LiteralNonNullExpression( s );

    public static ExpressionSyntax ParseExpressionSafe( string text )
    {
        var expression = SyntaxFactory.ParseExpression( text );

        var diagnostics = expression.GetDiagnostics().ToArray();

        if ( diagnostics.HasError() )
        {
            throw new DiagnosticException( $"Code '{text}' could not be parsed as an expression.", diagnostics.ToImmutableArray(), false );
        }

        return expression;
    }

    public static StatementSyntax ParseStatementSafe( string text )
    {
        var statement = SyntaxFactory.ParseStatement( text );

        var diagnostics = statement.GetDiagnostics();
        var enumerable = diagnostics as Diagnostic[] ?? diagnostics.ToArray();

        if ( enumerable.HasError() )
        {
            throw new DiagnosticException( $"Code could not be parsed as a statement.", enumerable.ToImmutableArray(), false );
        }

        return statement;
    }

    private static ExpressionSyntax EmptyExpression => SyntaxFactory.IdentifierName( SyntaxFactory.MissingToken( SyntaxKind.IdentifierToken ) );

    public static StatementSyntax EmptyStatement
        => SyntaxFactory.ExpressionStatement( EmptyExpression, SyntaxFactory.MissingToken( SyntaxKind.SemicolonToken ) );

    public static ExpressionStatementSyntax AssignmentStatement(
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
                SyntaxFactory.Token( default, SyntaxKind.SemicolonToken, syntaxGenerationContext.ElasticEndOfLineTriviaList ) )
            .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation );
}