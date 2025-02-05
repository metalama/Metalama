// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Code.SyntaxBuilders;

/// <summary>
/// Provides several ways to create instances of the <see cref="IExpression"/> interface.
/// </summary>
[CompileTime]
[PublicAPI]
public static class ExpressionFactory
{
    /// <summary>
    /// Returns an expression that represents a literal.
    /// </summary>
    /// <param name="value">A literal value, or <c>null</c> to represent a null string.</param>
    /// <param name="stronglyTyped">A value indicating if the literal should be qualified to remove any type ambiguity, for instance
    /// if the <paramref name="value"/> parameter contains a <c>long</c> that can also represent an <see cref="int"/>.</param> 
    public static IExpression Literal( object? value, bool stronglyTyped = false )
        => SyntaxBuilder.CurrentImplementation.Literal( value, SpecialType.None, false );

    /// <summary>
    /// Returns an expression that represents a literal of type <see cref="int"/>.
    /// </summary>
    public static IExpression Literal( int value ) => SyntaxBuilder.CurrentImplementation.Literal( value, SpecialType.Int32, false );

    /// <summary>
    /// Returns an expression that represents a literal of type <see cref="uint"/>.
    /// </summary>
    /// <param name="value">The literal value.</param>
    /// <param name="stronglyTyped">A value indicating if the literal should be qualified to remove any type ambiguity, for instance
    /// if the literal can also represent an <see cref="int"/>.</param>
    public static IExpression Literal( uint value, bool stronglyTyped = false )
        => SyntaxBuilder.CurrentImplementation.Literal( value, SpecialType.UInt32, stronglyTyped );

    /// <summary>
    /// Returns an expression that represents a literal of type <see cref="short"/>.
    /// </summary>
    /// <param name="value">The literal value.</param>
    /// <param name="stronglyTyped">A value indicating if the literal should be qualified to remove any type ambiguity, for instance
    /// if the literal can also represent an <see cref="int"/>.</param>
    public static IExpression Literal( short value, bool stronglyTyped = false )
        => SyntaxBuilder.CurrentImplementation.Literal( value, SpecialType.Int16, stronglyTyped );

    /// <summary>
    /// Returns an expression that represents a literal of type <see cref="ushort"/>.
    /// </summary>
    /// <param name="value">The literal value.</param>
    /// <param name="stronglyTyped">A value indicating if the literal should be qualified to remove any type ambiguity, for instance
    /// if the literal can also represent an <see cref="int"/>.</param>
    public static IExpression Literal( ushort value, bool stronglyTyped = false )
        => SyntaxBuilder.CurrentImplementation.Literal( value, SpecialType.UInt16, stronglyTyped );

    /// <summary>
    /// Returns an expression that represents a literal of type <see cref="long"/>.
    /// </summary>
    /// <param name="value">The literal value.</param>
    /// <param name="stronglyTyped">A value indicating if the literal should be qualified to remove any type ambiguity, for instance
    /// if the literal can also represent an <see cref="int"/>.</param>
    public static IExpression Literal( long value, bool stronglyTyped = false )
        => SyntaxBuilder.CurrentImplementation.Literal( value, SpecialType.Int64, stronglyTyped );

    /// <summary>
    /// Returns an expression that represents a literal of type <see cref="ulong"/>.
    /// </summary>
    /// <param name="value">The literal value.</param>
    /// <param name="stronglyTyped">A value indicating if the literal should be qualified to remove any type ambiguity, for instance
    /// if the literal can also represent an <see cref="int"/>.</param>
    public static IExpression Literal( ulong value, bool stronglyTyped = false )
        => SyntaxBuilder.CurrentImplementation.Literal( value, SpecialType.UInt64, stronglyTyped );

    /// <summary>
    /// Returns an expression that represents a literal of type <see cref="byte"/>.
    /// </summary>
    /// <param name="value">The literal value.</param>
    /// <param name="stronglyTyped">A value indicating if the literal should be qualified to remove any type ambiguity, for instance
    /// if the literal can also represent an <see cref="int"/>.</param>
    public static IExpression Literal( byte value, bool stronglyTyped = false )
        => SyntaxBuilder.CurrentImplementation.Literal( value, SpecialType.Byte, stronglyTyped );

    /// <summary>
    /// Returns an expression that represents a literal of type <see cref="sbyte"/>.
    /// </summary>
    /// <param name="value">The literal value.</param>
    /// <param name="stronglyTyped">A value indicating if the literal should be qualified to remove any type ambiguity, for instance
    /// if the literal can also represent an <see cref="int"/>.</param>
    public static IExpression Literal( sbyte value, bool stronglyTyped = false )
        => SyntaxBuilder.CurrentImplementation.Literal( value, SpecialType.SByte, stronglyTyped );

    /// <summary>
    /// Returns an expression that represents a literal of type <see cref="double"/>.
    /// </summary>
    /// <param name="value">The literal value.</param>
    /// <param name="stronglyTyped">A value indicating if the literal should be qualified to remove any type ambiguity, for instance
    /// if the literal can also represent an <see cref="int"/>.</param>
    public static IExpression Literal( double value, bool stronglyTyped = false )
        => SyntaxBuilder.CurrentImplementation.Literal( value, SpecialType.Double, stronglyTyped );

    /// <summary>
    /// Returns an expression that represents a literal of type <see cref="float"/>.
    /// </summary>
    /// <param name="value">The literal value.</param>
    /// <param name="stronglyTyped">A value indicating if the literal should be qualified to remove any type ambiguity, for instance
    /// if the literal can also represent an <see cref="int"/>.</param>
    public static IExpression Literal( float value, bool stronglyTyped = false )
        => SyntaxBuilder.CurrentImplementation.Literal( value, SpecialType.Single, stronglyTyped );

    /// <summary>
    /// Returns an expression that represents a literal of type <see cref="decimal"/>.
    /// </summary>
    /// <param name="value">The literal value.</param>
    /// <param name="stronglyTyped">A value indicating if the literal should be qualified to remove any type ambiguity, for instance
    /// if the literal can also represent an <see cref="int"/>.</param>
    public static IExpression Literal( decimal value, bool stronglyTyped = false )
        => SyntaxBuilder.CurrentImplementation.Literal( value, SpecialType.Decimal, stronglyTyped );

    /// <summary>
    /// Returns an expression that represents a literal of type <see cref="string"/>.
    /// </summary>
    /// <param name="value">The literal value.</param>
    /// <param name="stronglyTyped">A value indicating if the <c>null</c> value  should be qualified as <c>(string?) null</c>.</param>
    public static IExpression Literal( string? value, bool stronglyTyped = false )
        => SyntaxBuilder.CurrentImplementation.Literal( value, SpecialType.String, stronglyTyped );

    /// <summary>
    /// Parses a string containing a C# expression and returns an <see cref="IExpression"/>. The <see cref="IExpression.Value"/> property
    /// allows to use this expression in a template. An alternative to this method is the <see cref="ExpressionBuilder"/> class.
    /// </summary>
    /// <param name="code">A valid C# expression.</param>
    /// <param name="type">The resulting type of the expression, if known. This value allows to generate simpler code.</param>
    /// <param name="isReferenceable">Indicates whether the expression can be used in <c>ref</c> or <c>out</c> situations.</param>
    /// <seealso href="@templates"/>
    public static IExpression Parse( string code, IType? type = null, bool? isReferenceable = null )
        => SyntaxBuilder.CurrentImplementation.ParseExpression( code, type, isReferenceable );

    /// <summary>
    /// Creates a compile-time object that represents a run-time <i>expression</i>, i.e. the syntax or code, and not the result
    /// itself. The returned <see cref="IExpression"/> can then be used in run-time C# code thanks to the <see cref="IExpression.Value"/> property.
    /// This mechanism allows to generate expressions that depend on a compile-time control flow.
    /// </summary>
    /// <param name="expression">A run-time expression, possibly containing compile-time sub-expressions. The expression cannot be <c>dynamic</c>. If
    /// you have a dynamic expression, do not call this method, but cast the dynamic expression to <see cref="IExpression"/>.</param>
    /// <seealso href="@templates"/>
    [CompileTime( isTemplateOnly: true )]
    public static IExpression Capture( dynamic? expression ) => SyntaxBuilder.CurrentImplementation.Capture( (object?) expression );

    /// <summary>
    /// Returns an expression obtained by casting another expression to a type given as an <see cref="IType"/>.
    /// </summary>
    public static IExpression CastTo( this IExpression expression, IType targetType ) => SyntaxBuilder.CurrentImplementation.Cast( expression, targetType );

    /// <summary>
    /// Returns an expression obtained by casting another expression to a type given as a <see cref="Type"/>.
    /// </summary>
    public static IExpression CastTo( this IExpression expression, Type targetType ) => expression.CastTo( TypeFactory.GetType( targetType ) );

    /// <summary>
    /// Returns an expression obtained by casting another expression to a type given as a generic parameter.
    /// </summary>
    public static IExpression CastTo<T>( this IExpression expression ) => expression.CastTo( TypeFactory.GetType( typeof(T) ) );

    /// <summary>
    /// Gets a <c>this</c> expression for the given type.
    /// </summary>
    /// <param name="type">A type.</param>
    public static IExpression This( INamedType type ) => SyntaxBuilder.CurrentImplementation.ThisExpression( type );

    /// <summary>
    /// Gets a <c>this</c> expression for the current type when inside a template.
    /// </summary>
    public static IExpression This() => This( meta.Target.Type );

    /// <summary>
    /// Gets a <c>null</c> expression without specifying a type. The expression will be target-typed.
    /// </summary>
    public static IExpression Null() => SyntaxBuilder.CurrentImplementation.NullExpression( null );

    /// <summary>
    /// Gets a <c>null</c> expression for a given type.
    /// </summary>
    public static IExpression Null( IType? type ) => SyntaxBuilder.CurrentImplementation.NullExpression( type );

    /// <summary>
    /// Gets a <c>null</c> expression for a given type.
    /// </summary>
    public static IExpression Null( SpecialType type ) => Null( TypeFactory.GetType( type ) );

    /// <summary>
    /// Gets a <c>null</c> expression for a given type.
    /// </summary>
    public static IExpression Null( Type type ) => Null( TypeFactory.GetType( type ) );

    /// <summary>
    /// Gets a <c>null</c> expression for a given type.
    /// </summary>
    public static IExpression Null<T>() => Null( TypeFactory.GetType( typeof(T) ) );

    /// <summary>
    /// Gets a <c>default</c> expression without specifying a type. The expression will be target-typed.
    /// </summary>
    public static IExpression Default() => SyntaxBuilder.CurrentImplementation.DefaultExpression( null );

    /// <summary>
    /// Gets a <c>default</c> expression for a given type.
    /// </summary>
    public static IExpression Default( IType? type ) => SyntaxBuilder.CurrentImplementation.DefaultExpression( type );

    /// <summary>
    /// Gets a <c>default</c> expression for a given type.
    /// </summary>
    public static IExpression Default( SpecialType type ) => Default( TypeFactory.GetType( type ) );

    /// <summary>
    /// Gets a <c>default</c> expression for a given type.
    /// </summary>
    public static IExpression Default( Type type ) => Default( TypeFactory.GetType( type ) );

    /// <summary>
    /// Gets a <c>default</c> expression for a given type.
    /// </summary>
    public static IExpression Default<T>() => Default( TypeFactory.GetType( typeof(T) ) );

    /// <summary>
    /// Returns the same expression, but assuming it has a different type <see cref="IHasType.Type"/>. This method does not generate
    /// any cast (unlike <see cref="CastTo(Metalama.Framework.Code.IExpression,Metalama.Framework.Code.IType)"/>) and should only
    /// be used when the of the type given expression is wrongly infered.
    /// </summary>
    public static IExpression WithType( this IExpression expression, IType type ) => SyntaxBuilder.CurrentImplementation.WithType( expression, type );

    /// <summary>
    /// Returns the same expression, but assuming it has a different nullability. This method does not generate
    /// any cast (unlike <see cref="CastTo(Metalama.Framework.Code.IExpression,Metalama.Framework.Code.IType)"/>) and should only
    /// be used when the of the nullability given expression is wrongly infered.
    /// </summary>
    public static IExpression WithNullability( this IExpression expression, bool isNullable )
        => expression.WithType( isNullable ? expression.Type.ToNullable() : expression.Type.ToNonNullable() );
}