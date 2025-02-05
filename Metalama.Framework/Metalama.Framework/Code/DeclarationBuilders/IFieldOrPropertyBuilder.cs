// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Code.DeclarationBuilders
{
    /// <summary>
    /// Base interface for <see cref="IFieldBuilder"/> and <see cref="IPropertyBuilder"/>.
    /// </summary>
    public interface IFieldOrPropertyBuilder : IFieldOrProperty, IFieldOrPropertyOrIndexerBuilder
    {
        /// <summary>
        /// Gets or sets the initializer expression (i.e. the expression at the right hand of the equal sign).
        /// </summary>
        new IExpression? InitializerExpression { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the field or property is <c>required</c>, i.e. it must be initialized
        /// when an instance of the declaring type is initialized.
        /// </summary>
        new bool IsRequired { get; set; }
    }
}