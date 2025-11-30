// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Determines the scope (static vs instance) of a member introduced by an aspect.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This enum is used with the <see cref="IntroduceAttribute.Scope"/> property and the <c>scope</c> parameter of
    /// programmatic introduction methods like <see cref="AdviserExtensions.IntroduceMethod"/>.
    /// It controls whether the introduced member is static or instance.
    /// </para>
    /// </remarks>
    /// <seealso cref="IntroduceAttribute"/>
    /// <seealso cref="AdviserExtensions"/>
    /// <seealso href="@introducing-members"/>
    [CompileTime]
    public enum IntroductionScope
    {
        /// <summary>
        /// The scope is determined by the template: if the template member is static, the introduced member is static;
        /// if the template is non-static, the introduced member is an instance member.
        /// </summary>
        /// <remarks>
        /// This is the default behavior. Note that introducing an instance member to a static type results in an error.
        /// </remarks>
        Default,

        /// <summary>
        /// The introduced member is always an instance member, regardless of the template or target declaration.
        /// </summary>
        /// <remarks>
        /// Use this option when you need to introduce an instance member from a static template, or when you want to
        /// ensure the member is always an instance member regardless of the aspect target.
        /// </remarks>
        Instance,

        /// <summary>
        /// The introduced member is always a static member, regardless of the template or target declaration.
        /// </summary>
        /// <remarks>
        /// Use this option when you need to introduce a static member from an instance template, or when you want to
        /// ensure the member is always static regardless of the aspect target.
        /// </remarks>
        Static,

        /// <summary>
        /// The introduced member matches the scope of the target declaration to which the aspect is applied.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the aspect is applied to a static member or type, the introduced member is static.
        /// If the aspect is applied to an instance member, the introduced member is an instance member.
        /// </para>
        /// <para>
        /// This is useful when creating aspects that can be applied to both static and instance members.
        /// </para>
        /// </remarks>
        Target
    }
}