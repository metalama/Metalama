// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Specifies how introduction advice should behave when a member with the same name or signature already exists
    /// in the target type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This enumeration controls the conflict resolution strategy when introducing members (methods, properties, fields, events)
    /// or when implementing interfaces. The strategy determines whether to fail, skip, override, or hide the existing member.
    /// </para>
    /// <para>
    /// The choice of strategy affects both compilation behavior and the <see cref="AdviceOutcome"/> returned by the advice result.
    /// For example, <see cref="OverrideStrategy.Ignore"/> results in <see cref="AdviceOutcome.Ignore"/> when a conflict is detected, while
    /// <see cref="OverrideStrategy.Override"/> results in <see cref="AdviceOutcome.Override"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="IntroduceAttribute"/>
    /// <seealso cref="AdviserExtensions"/>
    /// <seealso cref="IAdviceFactory"/>
    /// <seealso cref="AdviceOutcome"/>
    /// <seealso href="@introducing-members"/>
    /// <seealso href="@implementing-interfaces"/>
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