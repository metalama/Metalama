// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code.Collections;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Represents a method, constructor, or indexer.
    /// </summary>
    public interface IHasParameters : IMember
    {
        /// <summary>
        /// Gets the list of parameters of the current method (but not the return parameter).
        /// </summary>
        IParameterList Parameters { get; }
    }
}