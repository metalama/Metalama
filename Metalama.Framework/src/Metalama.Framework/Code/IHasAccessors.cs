// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// A base interface for <see cref="IProperty"/> and <see cref="IEvent"/>. Exposes <see cref="GetAccessor"/>.
    /// </summary>
    public interface IHasAccessors : IMember, IHasType
    {
        /// <summary>
        /// Gets the accessor for a given <see cref="MethodKind"/>, or <c>null</c> if the member does not define
        /// an accessor of this kind.
        /// </summary>
        /// <param name="methodKind"><see cref="MethodKind.PropertyGet"/>, <see cref="MethodKind.PropertySet"/>,
        /// <see cref="MethodKind.EventAdd"/>, <see cref="MethodKind.EventRemove"/> or <see cref="MethodKind.EventRaise"/>.</param>
        /// <returns></returns>
        IMethod? GetAccessor( MethodKind methodKind );

        /// <summary>
        /// Gets the list of accessors defined by the current event or property.
        /// </summary>
        IEnumerable<IMethod> Accessors { get; }
    }
}