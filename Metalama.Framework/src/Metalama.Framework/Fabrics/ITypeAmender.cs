// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;

namespace Metalama.Framework.Fabrics
{
    /// <summary>
    /// The parameter passed to <see cref="TypeFabric.AmendType"/>.
    /// Provides capabilities to query members of the containing type, add advice (such as overriding or introducing members), configure options,
    /// report diagnostics, and validate architecture.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Through this interface, you can access all members of the containing type using LINQ-like queries through the <see cref="IAmender{T}"/> base interface,
    /// and add advice directly to the type's members using the <see cref="Advice"/> property. This allows type fabrics to function as type-level aspects
    /// without creating a separate reusable aspect class.
    /// </para>
    /// </remarks>
    /// <seealso cref="TypeFabric"/>
    /// <seealso cref="IAmender{T}"/>
    /// <seealso cref="INamedType"/>
    /// <seealso cref="IAdviceFactory"/>
    /// <seealso href="@fabrics"/>
    /// <seealso href="@fabrics-advising"/>
    /// <seealso href="@advising-code"/>
    /// <seealso href="@validation"/>
    public interface ITypeAmender : IAmender<INamedType>
    {
        /// <summary>
        /// Gets the target type of the current fabric (i.e. the declaring type of the nested type).
        /// </summary>
        INamedType Type { get; }

        /// <summary>
        /// Gets an object that provides methods for creating advice, such as overriding methods/properties/events, introducing new members,
        /// implementing interfaces, adding initializers, and applying contracts. This allows type fabrics to directly advise the containing type
        /// without creating a separate aspect.
        /// </summary>
        /// <seealso href="@advising-code"/>
        IAdviceFactory Advice { get; }
    }
}