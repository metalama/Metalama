// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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