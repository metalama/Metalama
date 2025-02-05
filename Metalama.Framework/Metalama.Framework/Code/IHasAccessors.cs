// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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