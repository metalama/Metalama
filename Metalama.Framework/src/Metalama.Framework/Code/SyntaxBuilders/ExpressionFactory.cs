// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Code.SyntaxBuilders;

/// <summary>
/// Provides factory methods to create <see cref="IExpression"/> objects, which are compile-time representations
/// of run-time C# expressions.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ExpressionFactory"/> class provides multiple approaches to creating <see cref="IExpression"/> objects:
/// <list type="bullet">
/// <item><see cref="Literal(object, bool)"/> and overloads - Create literal expressions from compile-time values</item>
/// <item><see cref="Parse"/> - Parse C# expression strings into <see cref="IExpression"/> objects</item>
/// <item><see cref="Capture"/> - Capture the syntax of a C# expression written in a template without evaluating it</item>
/// <item><see cref="This()"/>, <see cref="Null()"/>, <see cref="Default()"/> - Create common expressions like <c>this</c>, <c>null</c>, and <c>default</c></item>
/// </list>
/// For complex expressions that need programmatic construction, use <see cref="ExpressionBuilder"/> instead, which provides
/// a StringBuilder-like API for building expressions piece by piece.
/// </para>
/// </remarks>
/// <seealso cref="IExpression"/>
/// <seealso cref="ExpressionBuilder"/>
/// <seealso cref="IExpressionBuilder"/>
/// <seealso cref="TypedConstant"/>
/// <seealso href="@run-time-expressions"/>
/// <seealso href="@templates"/>
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
    [return: NotNullIfNotNull( nameof(code) )]
    public static IExpression? Parse( string? code, IType? type = null, bool? isReferenceable = null )
        => code == null ? null : SyntaxBuilder.CurrentImplementation.ParseExpression( code, type, isReferenceable );

    /// <summary>
    /// Creates a compile-time object that represents a run-time <i>expression</i>, i.e. the syntax or code, and not the result
    /// itself. The returned <see cref="IExpression"/> can then be used in run-time C# code thanks to the <see cref="IExpression.Value"/> property.
    /// This mechanism allows to generate expressions that depend on a compile-time control flow.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="Capture"/> method is the simplest way to create an <see cref="IExpression"/> in a template. It captures the
    /// C# syntax tree of an expression without evaluating it. This is particularly useful for creating expressions that depend on
    /// compile-time conditions.
    /// </para>
    /// <para>
    /// When the compile-time type of the expression to capture is <c>dynamic</c>, it must be explicitly cast to <see cref="IExpression"/>
    /// instead of using this method. This is a workaround for C# language limitations.
    /// </para>
    /// </remarks>
    /// <param name="expression">A run-time expression, possibly containing compile-time sub-expressions. The expression cannot be <c>dynamic</c>. If
    /// you have a dynamic expression, do not call this method, but cast the dynamic expression to <see cref="IExpression"/>.</param>
    /// <seealso href="@run-time-expressions"/>
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
    /// <remarks>
    /// Unlike <c>meta.This</c> and the parameterless <see cref="This()"/> method, the current method works in any context,
    /// even outside a template.
    /// </remarks>
    /// <seealso cref="meta.This"/>
    public static IExpression This( INamedType type ) => SyntaxBuilder.CurrentImplementation.ThisExpression( type );

    /// <summary>
    /// Gets a <c>this</c> expression for the current type when inside a template.
    /// </summary>
    /// <seealso cref="meta.This"/>
    public static IExpression This() => This( meta.Target.Type );

    /// <summary>
    /// Gets an expression representing the receiver, i.e. <c>this</c> in an instance member, or the receiver parameter in an extension member,
    /// according to the current context.
    /// </summary>
    /// <seealso cref="This()"/>
    public static IExpression Receiver() => SyntaxBuilder.CurrentImplementation.ReceiverExpression( meta.Target.Declaration );

    /// <summary>
    /// Gets an expression representing the receiver, i.e. <c>this</c> in an instance member, or the receiver parameter in an extension member,
    /// in the context of a given declaration.
    /// </summary>
    /// <remarks>
    /// Unlike the parameterless <see cref="Receiver()"/> method, the current method works in any context, even outside a template.
    /// </remarks> 
    /// <seealso cref="This(Metalama.Framework.Code.INamedType)"/>
    public static IExpression Receiver( IDeclaration declaration ) => SyntaxBuilder.CurrentImplementation.ReceiverExpression( declaration );

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
    /// be used when the of the type given expression is wrongly inferred.
    /// </summary>
    public static IExpression WithType( this IExpression expression, IType type ) => SyntaxBuilder.CurrentImplementation.WithType( expression, type );

    /// <summary>
    /// Returns the same expression, but assuming it has a different nullability. This method does not generate
    /// any cast (unlike <see cref="CastTo(Metalama.Framework.Code.IExpression,Metalama.Framework.Code.IType)"/>) and should only
    /// be used when the of the nullability given expression is wrongly inferred.
    /// </summary>
    public static IExpression WithNullability( this IExpression expression, bool isNullable )
        => expression.WithType( isNullable ? expression.Type.ToNullable() : expression.Type.ToNonNullable() );

    public static IExpression WithNullForgivingOperator( this IExpression expression, bool force = false )
        => SyntaxBuilder.CurrentImplementation.WithNullForgivingOperator( expression, force );
}