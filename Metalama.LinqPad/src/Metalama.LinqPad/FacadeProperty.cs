// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.LinqPad
{
    /// <summary>
    /// Represents a property in an <see cref="FacadeType"/>.
    /// </summary>
    /// <param name="Name"></param>
    /// <param name="Type"></param>
    /// <param name="GetFunc"></param>
    /// <param name="IsLazy"></param>
    internal sealed record FacadeProperty( string Name, Type Type, Func<object, object?> GetFunc, bool IsLazy = false );
}