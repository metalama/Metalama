// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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