// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Metalama.Framework.Engine.Extensibility.Transformations;

/// <summary>
/// Describes one argument of a rewritten call whose argument list differs from the source call: a value of the source call site, or an
/// expression emitted at the call site.
/// </summary>
/// <remarks>
/// <para>
/// The elements of <see cref="InvocationRedirectionRequest.Arguments"/> correspond to the parameters of the new target in order, after the
/// receiver parameter when the receiver mode passes the receiver. The linker writes every argument as a named argument, so it can keep the order of
/// evaluation of the source call site: the values of the source call site are written, and therefore evaluated, in their source order, and the
/// expressions after them.
/// </para>
/// <para>
/// A value of the source call site that the list does not use is still evaluated, in its source order, when it can have a side effect. The linker
/// evaluates it as the governing expression of a switch expression whose value is the next argument, <c>D switch { _ =&gt; value }</c>, or, when
/// the next argument cannot hold it, after the previous argument, <c>value switch { var t =&gt; D switch { _ =&gt; t } }</c>. The factory refuses
/// a request when the omitted value is passed by reference, when no adjacent argument is passed by value, or when the language version is earlier
/// than C# 9. A value without side effect, such as a constant or a local, is not evaluated.
/// </para>
/// </remarks>
[PublicAPI]
public sealed class RedirectedArgument
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RedirectedArgument"/> class. Use the static members of the class to create an instance.
    /// </summary>
    private RedirectedArgument( RedirectedArgumentKind kind, int parameterOrdinal, ExpressionSyntax? expression, string? name, IType? castType = null )
    {
        this.Kind = kind;
        this.ParameterOrdinal = parameterOrdinal;
        this.Expression = expression;
        this.Name = name;
        this.CastType = castType;
    }

    /// <summary>
    /// Gets an argument that passes the receiver of the source call site.
    /// </summary>
    public static RedirectedArgument SourceReceiver { get; } = new( RedirectedArgumentKind.SourceReceiver, -1, null, null );

    /// <summary>
    /// Gets an argument that passes the argument written at the source call site for a parameter of the source method.
    /// </summary>
    /// <param name="parameterOrdinal">The ordinal of the parameter of the source method. The argument must be written at the source call site. When
    /// the call site passes the elements of a <c>params</c> parameter in expanded form, the elements are packed into one collection, <c>[e1, e2]</c>,
    /// or an array creation before C# 12.</param>
    public static RedirectedArgument SourceArgument( int parameterOrdinal )
    {
        if ( parameterOrdinal < 0 )
        {
            throw new ArgumentOutOfRangeException( nameof(parameterOrdinal) );
        }

        return new RedirectedArgument( RedirectedArgumentKind.SourceArgument, parameterOrdinal, null, null );
    }

    /// <summary>
    /// Gets an argument that passes an expression, which is emitted at the call site after the values of the source call site.
    /// </summary>
    /// <param name="expression">The expression. It must bind at the call site.</param>
    public static RedirectedArgument Value( ExpressionSyntax expression ) => new( RedirectedArgumentKind.Value, -1, expression, null );

    /// <summary>
    /// Returns a copy of this argument that is written with the given parameter name instead of the name of the corresponding parameter of the
    /// target.
    /// </summary>
    public RedirectedArgument WithName( string parameterName ) => new( this.Kind, this.ParameterOrdinal, this.Expression, parameterName, this.CastType );

    /// <summary>
    /// Returns a copy of this argument whose value is cast to the given type before it is passed, <c>(T)(value)</c>.
    /// </summary>
    /// <remarks>
    /// A source call site converts an argument to the type of the parameter of the source method. When the parameter of the new target has another
    /// type, the conversion of the argument to that type can differ. For instance, an <see cref="int"/> value passed to a <see cref="long"/>
    /// parameter of the source method is boxed as an <see cref="int"/> when it is passed to an <see cref="object"/> parameter of the target. A cast
    /// to the type of the parameter of the source method keeps the conversion of the source call site. The factory accepts a cast only on a source
    /// argument that is passed by value.
    /// </remarks>
    /// <param name="type">The type to which the value is cast, typically the type of the parameter of the source method.</param>
    public RedirectedArgument WithCast( IType type ) => new( this.Kind, this.ParameterOrdinal, this.Expression, this.Name, type );

    /// <summary>
    /// Gets the kind of the argument.
    /// </summary>
    public RedirectedArgumentKind Kind { get; }

    /// <summary>
    /// Gets the ordinal of the parameter of the source method, for <see cref="RedirectedArgumentKind.SourceArgument"/>, and -1 otherwise.
    /// </summary>
    public int ParameterOrdinal { get; }

    /// <summary>
    /// Gets the expression, for <see cref="RedirectedArgumentKind.Value"/>, and <c>null</c> otherwise.
    /// </summary>
    public ExpressionSyntax? Expression { get; }

    /// <summary>
    /// Gets the name with which the argument is written, or <c>null</c> to use the name of the corresponding parameter of the target.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the type to which the value is cast before it is passed, or <c>null</c> when the value is passed without a cast.
    /// </summary>
    public IType? CastType { get; }
}
