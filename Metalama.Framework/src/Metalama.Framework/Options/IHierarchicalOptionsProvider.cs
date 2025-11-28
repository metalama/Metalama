// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Collections.Generic;

namespace Metalama.Framework.Options;

/// <summary>
/// A base interface for attributes and aspects that provide options.
/// </summary>
/// <remarks>
/// <para>
/// This interface allows custom attributes and aspects to supply hierarchical options that will be merged with other options
/// in the configuration system. This is typically used to expose options directly on an aspect's custom attribute, allowing users
/// to configure the aspect's behavior when they apply it.
/// </para>
/// <para>
/// When implementing this interface, the <see cref="GetOptions"/> method should return one or more option objects that implement
/// <see cref="IHierarchicalOptions"/>. These options will be merged with options from other sources (such as fabrics) according
/// to the rules defined in <see cref="ApplyChangesAxis"/>.
/// </para>
/// </remarks>
/// <seealso cref="IHierarchicalOptions"/>
/// <seealso cref="IHierarchicalOptions{T}"/>
/// <seealso cref="IAspect"/>
/// <seealso href="@exposing-options"/>
/// <seealso href="@aspect-configuration"/>
[RunTimeOrCompileTime]
public interface IHierarchicalOptionsProvider
{
    /// <summary>
    /// Gets the list of options provided by the current aspect or attribute.
    /// </summary>
    /// <param name="context">The context providing information about the target declaration and diagnostic services.</param>
    /// <returns>The list of options provided by this aspect or attribute. Typically, this returns a single option object wrapped in an array.</returns>
    /// <remarks>
    /// <para>
    ///     This interface behaves differently when applied to plain custom attributes than when applied to aspects.
    /// </para>
    /// <para>
    ///     When applied to plain custom attributes, the <see cref="GetOptions"/> method is invoked immediately in the first stage
    ///     of the compilation process, therefore the provided options are immediately available for readers.
    /// </para>
    /// <para>
    ///     However, when the interface is implemented by an aspect, i.e. any class implementing the <see cref="IAspect"/> interface,
    ///     the <see cref="GetOptions"/> method is called right before the <see cref="IAspect{T}.BuildAspect"/> method of the aspect
    ///     is invoked. The provided options are therefore only available to the current aspect instance and any code executing after
    ///     this aspect instance.
    /// </para>
    /// <para>
    ///     Because custom attributes cannot have properties of nullable value types, aspects that expose options typically use field-backed
    ///     properties where nullable fields store the underlying values, allowing the aspect to distinguish between default values and unspecified values.
    /// </para>
    /// </remarks>
    IEnumerable<IHierarchicalOptions> GetOptions( in OptionsProviderContext context );
}