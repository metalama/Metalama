// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;

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
        /// use the <see cref="With(Metalama.Framework.Code.Invokers.InvokerOptions)"/> method.
        /// </summary>
        dynamic? GetValue( params dynamic?[] args );

        /// <summary>
        /// Generates run-time code that sets the value of the current indexer with specified arguments. By default, the target instance
        /// of the indexer is <c>this</c> unless the indexer is static, and the <c>base</c> implementation of the indexer is invoked,
        /// i.e. the implementation before the current aspect layer. To change the default values, or to use the <c>?.</c> null-conditional operator,
        /// use the <see cref="With(Metalama.Framework.Code.Invokers.InvokerOptions)"/> method.
        /// </summary>
        /// <remarks>
        /// Note: the order of parameters is different than in C# code:
        /// e.g. <c>instance[args] = value</c> is <c>indexer.SetIndexerValue(instance, value, args)</c>.
        /// </remarks>
        dynamic? SetValue( dynamic? value, params dynamic?[] args );

        /// <summary>
        /// Gets an <see cref="IIndexerInvoker"/> for the same index and target but with different options.
        /// </summary>
        IIndexerInvoker With( InvokerOptions options );

        /// <summary>
        /// Gets an <see cref="IIndexerInvoker"/> for the same indexer but with a different field or property and with different options.
        /// </summary>
        IIndexerInvoker With( dynamic? target, InvokerOptions options = default );
    }
}