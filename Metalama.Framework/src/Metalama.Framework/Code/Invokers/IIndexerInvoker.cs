// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Code.Invokers
{
    /// <summary>
    /// Allows generating run-time code that accesses indexer items when you have the compile-time <see cref="IIndexer"/> representation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Invokers are APIs that allow you to generate run-time code from compile-time declarations. When you have an <see cref="IIndexer"/>
    /// object (a compile-time representation of an indexer), you can use its indexer syntax to create <see cref="IExpression"/> objects
    /// that represent indexed access. The returned expression can be used to get or set indexer values in template code via the
    /// <see cref="IExpression.Value"/> property.
    /// </para>
    /// <para>
    /// Use <see cref="WithObject"/> to specify the target instance and <see cref="WithOptions"/> to control nullability behavior
    /// (such as using the <c>?.</c> operator) and which implementation layer (base, current, or final) to access.
    /// </para>
    /// <para>
    /// By default, the indexer is accessed on the current object (<c>this</c>) for instance indexers. The <c>base</c> implementation
    /// is accessed, i.e. the implementation <i>before</i> the current aspect layer.
    /// </para>
    /// </remarks>
    /// <seealso cref="IIndexer"/>
    /// <seealso cref="InvokerOptions"/>
    /// <seealso cref="IExpression"/>
    /// <seealso href="@invokers"/>
    /// <seealso href="@dynamic-typing"/>
    [CompileTime]
    public interface IIndexerInvoker
    {
        /// <summary>
        /// Gets an <see cref="IExpression"/> representing an item, with arguments represented as a list of <see cref="IExpression"/>.
        /// </summary>
        /// <param name="args">A list of <see cref="IExpression"/> to be passed to the indexer.</param>
        /// <returns>An <see cref="IExpression"/> representing the specified item.</returns> 
        /// <remarks>
        /// By default, the indexer is accessed on the current object (<c>this</c>), unless it is static. The <c>base</c> implementation 
        /// of the indexer is invoked, i.e. the implementation <i>before</i> the current aspect layer. To change the default values,
        /// or to use the <c>?</c> null-conditional operator, use the <see cref="WithOptions"/> method.
        /// </remarks> 
        IExpression this[ params IExpression[] args ] { get; }

        /// <summary>
        /// Gets an <see cref="IExpression"/> representing an item, with arguments represented as run-time C# expressions. 
        /// </summary>
        /// <param name="args">A list of run-time C# expressions to be passed to the indexer. If the compile-time type
        /// of any expression is <c>dynamic</c>, it must be explicitly cast to <see cref="IExpression"/>.</param>
        /// <returns>An <see cref="IExpression"/> representing the specified item.</returns>
        /// <remarks>
        /// By default, the indexer is accessed on the current object (<c>this</c>), unless it is static. The <c>base</c> implementation 
        /// of the indexer is invoked, i.e. the implementation <i>before</i> the current aspect layer. To change the default values,
        /// or to use the <c>?</c> null-conditional operator, use the <see cref="WithOptions"/> method.
        /// </remarks> 
        IExpression this[ params dynamic?[] args ] { get; }

        /// <summary>
        /// Gets an <see cref="IIndexerInvoker"/> for the same index and target but with different options.
        /// </summary>
        IIndexerInvoker WithOptions( InvokerOptions options );

        /// <summary>
        /// Gets an <see cref="IIndexerInvoker"/> for the same method but with a different object, provided as a C# expression.
        /// </summary>
        /// <param name="obj">The run-time C# expression that represents the object on which the indexer should be accessed. If the compile-time type
        ///     of the expression is <c>dynamic</c>, it must be explicitly cast to <see cref="IExpression"/>.</param>
        IIndexerInvoker WithObject( dynamic obj );

        /// <summary>
        /// Gets an <see cref="IIndexerInvoker"/> for the same method but with a different object, provided as a compile-time <see cref="IExpression"/>.
        /// </summary>
        /// <param name="obj">The <see cref="IExpression"/> that represents the object on which the indexer should be accessed.</param>
        IIndexerInvoker WithObject( IExpression obj );

        [Obsolete( "Use the Item[] member, returning an IExpression, then access IExpression.Value." )]
        dynamic? GetValue( params dynamic?[] args );

        [Obsolete( "Use the Item[] member, returning an IExpression, then assign IExpression.Value." )]
        dynamic? SetValue( dynamic? value, params dynamic?[] args );

        [Obsolete( "Use the WithOptions method." )]
        IIndexerInvoker With( InvokerOptions options );

        [Obsolete( "Use the WithObject and WithOptions methods." )]
        IIndexerInvoker With( dynamic? target, InvokerOptions options = default );
    }
}