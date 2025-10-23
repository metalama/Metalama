// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Code.Invokers
{
    /// <summary>
    /// Allows accessing the value of indexers.
    /// </summary>
    [CompileTime]
    public interface IIndexerInvoker
    {
        /// <summary>
        /// Generates run-time code that gets the value of the current indexer with specified arguments. By default, the target instance
        /// of the indexer is <c>this</c> unless the indexer is static, and the <c>base</c> implementation of the indexer is invoked,
        /// i.e. the implementation before the current aspect layer. To change the default values, or to use the <c>?.</c> null-conditional operator,
        /// use the <see cref="WithOptions"/> method.
        /// </summary>
        [Obsolete("Use the Item[] member, returning an IExpression.")]
        dynamic? GetValue( params dynamic?[] args );
        
        /// <summary>
        /// Generates run-time code that sets the value of the current indexer with specified arguments. By default, the target instance
        /// of the indexer is <c>this</c> unless the indexer is static, and the <c>base</c> implementation of the indexer is invoked,
        /// i.e. the implementation before the current aspect layer. To change the default values, or to use the <c>?.</c> null-conditional operator,
        /// use the <see cref="WithOptions"/> method.
        /// </summary>
        /// <remarks>
        /// Note: the order of parameters is different than in C# code:
        /// e.g. <c>instance[args] = value</c> is <c>indexer.SetIndexerValue(instance, value, args)</c>.
        /// </remarks>
        [Obsolete("Use the Item[] member, returning an IExpression.")]
        dynamic? SetValue( dynamic? value, params dynamic?[] args );
        
        IExpression this[ params IExpression[] args ] { get; }
        
        IExpression this[ params dynamic?[] args ] { get; }

        /// <summary>
        /// Gets an <see cref="IIndexerInvoker"/> for the same index and target but with different options.
        /// </summary>
        IIndexerInvoker WithOptions( InvokerOptions options );

        /// <summary>
        /// Gets an <see cref="IIndexerInvoker"/> for the same indexer but with a different field or property and with different options.
        /// </summary>
        IIndexerInvoker WithObject( dynamic? target );

        [Obsolete( "Use the WithOptions method." )]
        IIndexerInvoker With( InvokerOptions options );

        [Obsolete( "Use the WithObject and WithOptions methods." )]
        IIndexerInvoker With( dynamic? target, InvokerOptions options = default );
    }
}