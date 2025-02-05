// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Code.DeclarationBuilders
{
    /// <summary>
    /// Allows to complete the construction of a property that has been created by an advice.
    /// </summary>
    public interface IPropertyBuilder : IFieldOrPropertyBuilder, IProperty, IPropertyOrIndexerBuilder;
}