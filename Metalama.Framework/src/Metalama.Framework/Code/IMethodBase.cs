// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Reflection;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Represents a method or a constructor.
    /// </summary>
    public interface IMethodBase : IHasParameters
    {
        /// <summary>
        /// Gets a <see cref="MethodBase"/> that represents the current method or constructor at run time.
        /// </summary>
        /// <returns>A <see cref="MethodBase"/> that can be used only in run-time code.</returns>
        MethodBase ToMethodBase();

        new IRef<IMethodBase> ToRef();
    }
}