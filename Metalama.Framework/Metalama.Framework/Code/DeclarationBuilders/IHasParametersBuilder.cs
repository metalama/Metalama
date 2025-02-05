// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Code.DeclarationBuilders
{
    /// <summary>
    /// Represents a builder of method, constructor, or indexer. Overrides the <see cref="Parameters"/> property to allow
    /// using <see cref="IParameterBuilderList"/> interface.
    /// </summary>
    public interface IHasParametersBuilder : IMemberBuilder, IHasParameters
    {
        /// <summary>
        /// Gets the list of parameters of the current method (but not the return parameter).
        /// </summary>
        new IParameterBuilderList Parameters { get; }
    }
}