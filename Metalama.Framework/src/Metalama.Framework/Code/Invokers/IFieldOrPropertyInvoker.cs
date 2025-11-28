// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Code.Invokers
{
    /// <summary>
    /// Allows generating run-time code that accesses a field or property when you have its compile-time <see cref="IFieldOrProperty"/> representation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Invokers are APIs that allow you to generate run-time code from compile-time declarations. When you have an <see cref="IFieldOrProperty"/>
    /// object (a compile-time representation of a field or property), you can use it as an <see cref="IExpression"/> to access its value
    /// in generated code. Since this interface implements <see cref="IExpression"/>, you can use the <see cref="IExpression.Value"/>
    /// property to get or set the field or property value directly in template code.
    /// </para>
    /// <para>
    /// For fields, you can also use <c>ref</c> when accessing the <see cref="IExpression.Value"/> property, allowing you to pass fields
    /// by reference. Use <see cref="WithObject(dynamic?)"/> to specify the target instance and <see cref="WithOptions"/> to control nullability
    /// behavior and which implementation layer (base, current, or final) to access.
    /// </para>
    /// <para>
    /// By default, the field or property is accessed on the current object (<c>this</c>) for instance members. The <c>base</c>
    /// implementation is accessed, i.e. the implementation <i>before</i> the current aspect layer.
    /// </para>
    /// </remarks>
    /// <seealso cref="IFieldOrProperty"/>
    /// <seealso cref="InvokerOptions"/>
    /// <seealso cref="IExpression"/>
    /// <seealso href="@invokers"/>
    /// <seealso href="@dynamic-typing"/>
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