// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code.Invokers
{
    /// <summary>
    /// Allows accessing the the value of fields or properties through the <see cref="IExpression.Value"/> property of
    /// the <see cref="IExpression"/> interface. By default, the target instance
    /// of the field or property is <c>this</c> unless the property is static, and the <c>base</c> implementation of the property is invoked,
    /// i.e. the implementation before the current aspect layer. To change the default values, or to use the <c>?.</c> null-conditional operator,
    /// use the <see cref="With(Metalama.Framework.Code.Invokers.InvokerOptions)"/> method.
    /// </summary>
    [CompileTime]
    public interface IFieldOrPropertyInvoker : IExpression
    {
        /// <summary>
        /// Gets an <see cref="IFieldOrPropertyInvoker"/> for the same field or property and target but with different options.
        /// </summary>
        IFieldOrPropertyInvoker With( InvokerOptions options );

        /// <summary>
        /// Gets an <see cref="IFieldOrPropertyInvoker"/> for the same field or property but with a different field or property and with different options.
        /// </summary>
        IFieldOrPropertyInvoker With( dynamic? target, InvokerOptions options = default );
    }
}