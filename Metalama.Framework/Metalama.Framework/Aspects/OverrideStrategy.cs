// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Conflict behavior of introduction advice.
    /// </summary>
    [CompileTime]
    public enum OverrideStrategy
    {
        /// <summary>
        /// The advice fails with a compilation error if the member already exists in the target declaration. Same as <see cref="Fail"/>.
        /// </summary>
        Default = Fail,

        /// <summary>
        /// The advice fails with a compilation error if the member exists in the target declaration.
        /// </summary>
        Fail = 0,

        /// <summary>
        /// Advice is ignored if the member already exists in the target declaration.
        /// </summary>
        Ignore = 1,

        /// <summary>
        /// Advice attempts to override the existing member or fails with a compilation error if that is not possible.
        /// </summary>
        /// <remarks>
        /// For attributes, this first removes all existing instances of the attribute.
        /// </remarks>
        Override = 2,

        /// <summary>
        /// If the member already exists, the advice attempts to redefine it using <c>new</c> or fails with a compilation error if that is not possible.
        /// </summary>
        /// <remarks>
        /// For attributes, this adds an instance of the attribute, even when some already exist.
        /// </remarks>
        New = 3

        /*
        // TODO: What happens if the there is a conflict while merging members?

        /// <summary>
        /// If the member already exists, the advice attempts to merge the introduced type with the target type. For non-type introductions the behavior is the same as <see cref="Ignore"/>.
        /// Merging is done by introducing individual member of the template into the target type.
        /// </summary>
        Merge = 4
        */
    }
}