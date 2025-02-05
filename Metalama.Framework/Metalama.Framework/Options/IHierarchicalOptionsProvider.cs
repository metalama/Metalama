// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using System.Collections.Generic;

namespace Metalama.Framework.Options;

/// <summary>
/// A base interface for attributes that provide options.
/// </summary>
/// <seealso href="@exposing-options"/>
[RunTimeOrCompileTime]
public interface IHierarchicalOptionsProvider
{
    /// <summary>
    /// Gets the list of options provided by the current aspect or attribute.
    /// </summary>
    /// <param name="context"></param>
    /// <returns>The list of options.</returns>
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
    /// </remarks>
    IEnumerable<IHierarchicalOptions> GetOptions( in OptionsProviderContext context );
}