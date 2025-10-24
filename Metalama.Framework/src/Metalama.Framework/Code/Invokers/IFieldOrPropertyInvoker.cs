// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Code.Invokers
{
    /// <summary>
    /// Allows accessing the value of fields or properties through the <see cref="IExpression.Value"/> property of
    /// the <see cref="IExpression"/> interface. This interface augments <see cref="IExpression"/> with methods that allow to control
    /// the object on which the field or property is accessed, as well as nullability options (see remarks).
    /// </summary>
    /// <remarks>
    /// By default, the property is accessed on the current object (<c>this</c>), unless the property is static. The <c>base</c> implementation 
    /// of the property is invoked, i.e. the implementation <i>before</i> the current aspect layer. To change the default values,
    /// use the <see cref="WithOptions"/> method.
    /// </remarks>
    [CompileTime]
    public interface IFieldOrPropertyInvoker : IExpression
    {
        /// <summary>
        /// Gets an <see cref="IFieldOrPropertyInvoker"/> for the same field or property and target but with different options.
        /// </summary>
        IFieldOrPropertyInvoker WithOptions( InvokerOptions options );

        /// <summary>
        /// Gets an <see cref="IFieldOrPropertyInvoker"/> for the same field or property but with a different object.
        /// </summary>
        IFieldOrPropertyInvoker WithObject( IExpression? target );

        IFieldOrPropertyInvoker WithObject( dynamic? target );

        [Obsolete( "Use the WithOptions method." )]
        IFieldOrPropertyInvoker With( InvokerOptions options );

        [Obsolete( "Use the WithObject and WithOptions methods." )]
        IFieldOrPropertyInvoker With( dynamic? target, InvokerOptions options = default );
    }
}