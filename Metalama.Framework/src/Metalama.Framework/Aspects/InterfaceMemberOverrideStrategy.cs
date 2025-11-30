// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Determines how to handle conflicts when introducing an interface member via <see cref="InterfaceMemberAttribute"/>
    /// when a member with the same name already exists on the target type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this enum with <see cref="InterfaceMemberAttribute.WhenExists"/> to control per-member conflict resolution,
    /// overriding the default behavior specified by the <see cref="OverrideStrategy"/> parameter of
    /// <see cref="AdviserExtensions.ImplementInterface(IAdviser{INamedType}, INamedType, OverrideStrategy, object?)"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="InterfaceMemberAttribute"/>
    /// <seealso cref="OverrideStrategy"/>
    /// <seealso cref="AdviserExtensions.ImplementInterface(IAdviser{INamedType}, INamedType, OverrideStrategy, object?)"/>
    /// <seealso href="@implementing-interfaces"/>
    [CompileTime]
    public enum InterfaceMemberOverrideStrategy
    {
        /// <summary>
        /// Use the strategy from the <see cref="OverrideStrategy"/> parameter of
        /// <see cref="IAdviceFactory.ImplementInterface(INamedType,INamedType,OverrideStrategy,object?)"/>.
        /// </summary>
        /// <remarks>
        /// When the interface-level strategy is <see cref="OverrideStrategy.Fail"/>, this member fails on conflict.
        /// When it is <see cref="OverrideStrategy.Override"/>, the existing member is overridden.
        /// </remarks>
        Default = 0,

        /// <summary>
        /// Report a compilation error if a matching member already exists in the target type.
        /// </summary>
        Fail,

        /// <summary>
        /// Introduce the interface member as an explicit implementation, avoiding the conflict with the existing member.
        /// </summary>
        /// <remarks>
        /// Use this when the target type already has a public member with the same name but you want to provide
        /// a different implementation for the interface member.
        /// </remarks>
        MakeExplicit,

        /// <summary>
        /// Do not introduce or override this member if a matching member already exists.
        /// </summary>
        /// <remarks>
        /// Use this to allow the existing implementation to satisfy the interface member requirement.
        /// </remarks>
        Ignore = 3

        // TODO: Support.
        //       The problem is that these are not really useful when the other declaration is not compatible.
        //       If the existing declaration has a different return type, it's not usable, leading to an error. User can solve this only programatically.
        //       The name of this enum however implies that we can override.

        // /// <summary>
        // /// The advice uses the existing type member if it exactly matches the interface member and ignores the provided template, otherwise the advice fails with a compilation error.
        // /// </summary>
        // UseExisting,

        // /// <summary>
        // /// The advice overrides the target declaration using the template specified for the interface member. The advice fails with a compilation error if it is not possible.
        // /// </summary>
        // Override,
    }
}