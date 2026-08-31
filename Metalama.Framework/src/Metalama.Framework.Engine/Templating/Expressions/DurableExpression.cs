// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Metalama.Framework.Engine.Templating.Expressions;

/// <summary>
/// The implementation of <see cref="IDurableExpression"/>: the expression rendered to syntax, plus a durable
/// reference to its type.
/// </summary>
/// <remarks>
/// <para>
/// The expression is rendered when this object is created, not when it is used. Rendering later would need the
/// original expression, and two kinds cannot be stored: <c>DelegateUserExpression</c> holds a <c>Func</c>, and
/// <c>CapturedUserExpression</c> holds an object boxed by the template compiler.
/// </para>
/// <para>
/// The cost is the <c>targetType</c> argument of <c>UserExpression.ToSyntax</c>, which is unknown at this point. Only
/// the kinds that override <c>GetSyntaxType</c> use it; a method group, for example, then renders as its own type
/// rather than as the target delegate type. Conversion, formatting and simplification are unaffected: they still run
/// at the use site.
/// </para>
/// <para>
/// The syntax is stored rather than its text so that the annotations on its nodes survive. <c>ToSyntax</c> returns
/// deliberately over-specified syntax, and those annotations are what let the formatter simplify it later.
/// </para>
/// </remarks>
internal sealed class DurableExpression : IDurableExpression
{
    /// <remarks>
    /// Always detached, so that it reaches no syntax tree. Generated nodes already are. Nodes read from the code
    /// model are not: <c>SourceField</c>, <c>SourceProperty</c> and <c>SourceEvent</c> return the initializer of the
    /// declaration, which belongs to the tree of its source file.
    /// </remarks>
#pragma warning disable LAMA0870
    private readonly ExpressionSyntax _syntax;
#pragma warning restore LAMA0870

    public DurableExpression( ExpressionSyntax syntax, IType type, bool? isReferenceable, bool isAssignable )
        : this( syntax, type.ToDurableRef(), isReferenceable, isAssignable ) { }

    private DurableExpression( ExpressionSyntax syntax, IDurableRef<IType> type, bool? isReferenceable, bool isAssignable )
    {
        this._syntax = Detach( syntax );
        this.Type = type;
        this.IsReferenceable = isReferenceable;
        this.IsAssignable = isAssignable;
    }

    /// <summary>
    /// Creates an instance from the values a serializer persisted.
    /// </summary>
    /// <remarks>
    /// The annotations are the ones <c>SyntaxBuilderImpl.ParseExpression</c> adds, so that a deserialized expression
    /// is formatted like one the user parsed from a string.
    /// </remarks>
    internal static DurableExpression FromText( string text, IDurableRef<IType> type, bool? isReferenceable, bool isAssignable )
        => new(
            SyntaxFactoryEx.ParseExpressionSafe( text )
                .WithAdditionalAnnotations( Formatter.Annotation )
                .WithSimplifierAnnotation(),
            type,
            isReferenceable,
            isAssignable );

    public IDurableRef<IType> Type { get; }

    /// <summary>
    /// Gets a value indicating whether the expression can be used in a <c>ref</c> or <c>out</c> position.
    /// </summary>
    /// <remarks>
    /// Not on <see cref="IDurableExpression"/>, because <see cref="IExpression"/> does not expose it either.
    /// </remarks>
    internal bool? IsReferenceable { get; }

    public bool IsAssignable { get; }

    public string Text => this._syntax.ToString();

    public IExpression ToExpression( ICompilation compilation )
        => new SyntaxUserExpression( this._syntax, this.Type.GetTarget( compilation ), this.IsReferenceable, this.IsAssignable );

    /// <summary>
    /// Returns a node that belongs to no tree, re-parsing it if necessary.
    /// </summary>
    /// <remarks>
    /// An <see cref="ExpressionSyntax"/> is never the root of a parsed tree, so a node without a parent was built
    /// rather than read, and is already detached. Re-parsing loses annotations, but only nodes read from source need
    /// it, and those carry none.
    /// </remarks>
    private static ExpressionSyntax Detach( ExpressionSyntax syntax )
        => syntax.Parent == null ? syntax : SyntaxFactoryEx.ParseExpressionSafe( syntax.ToString() );

    public override string ToString() => this.Text;
}