// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Code.Collections
{
    public interface IMemberCollection<out T> : IMemberOrNamedTypeCollection<T>
        where T : IMember
    {
        INamedType DeclaringType { get; }
    }
}