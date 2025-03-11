// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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