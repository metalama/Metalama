// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.RunTime;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// A base interface for <see cref="IField"/> and <see cref="IProperty"/>.
    /// </summary>
    public interface IFieldOrProperty : IFieldOrPropertyOrIndexer, IFieldOrPropertyInvoker
    {
        /// <summary>
        /// Gets a value indicating whether the declaration is an auto-property or a field, or <c>null</c> if the
        /// implementation of the property cannot be determined, for instance for properties in a referenced assembly.
        /// </summary>
        /// <remarks>
        /// When an automatic property has been overridden, this property will still return <c>true</c>, even if the actual
        /// property implementation no longer corresponds to the one of an automatic property.
        /// </remarks>
        bool? IsAutoPropertyOrField { get; }

        /// <summary>
        /// Gets a <see cref="FieldOrPropertyInfo"/> that represents the current field or property at run time.
        /// </summary>
        /// <returns>A <see cref="FieldOrPropertyInfo"/> that can be used only in run-time code.</returns>
        [CompileTimeReturningRunTime]
        FieldOrPropertyInfo ToFieldOrPropertyInfo();

        /// <summary>
        /// Gets a value indicating whether the field or property is <c>required</c>, i.e. it must be initialized
        /// when an instance of the declaring type is initialized.
        /// </summary>
        bool IsRequired { get; }

        /// <summary>
        /// Gets the initializer expression (i.e. the expression at the right hand of the equal sign), if any.
        /// When the field or property is defined in source code, this property returns an <see cref="ISourceExpression"/>, which
        /// exposes a <see cref="TypedConstant"/> when possible.
        /// </summary>
        IExpression? InitializerExpression { get; }

        new IRef<IFieldOrProperty> ToRef();
    }
}