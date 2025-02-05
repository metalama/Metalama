// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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