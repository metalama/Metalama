// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code.DeclarationBuilders
{
    /// <summary>
    /// Allows to complete the construction of a property that has been created by an advice.
    /// </summary>
    /// <seealso cref="IProperty"/>
    /// <seealso cref="IFieldOrPropertyBuilder"/>
    /// <seealso cref="AdviserExtensions.IntroduceProperty(IAdviser{INamedType}, string, IntroductionScope, OverrideStrategy, System.Action{IPropertyBuilder}?, object?)"/>
    /// <seealso href="@introducing-members"/>
    public interface IPropertyBuilder : IFieldOrPropertyBuilder, IProperty, IPropertyOrIndexerBuilder;
}