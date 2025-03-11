// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using PostSharp.Reflection;

namespace PostSharp.Aspects.Advices
{
    /// <summary>
    /// In Metalama, call one of the <c>Introduce</c> methods of
    ///  <c>builder</c>.<see cref="IAspectBuilder.Advice"/>.
    /// </summary>
    /// <seealso href="@introducing-members"/>
    [PublicAPI]
    public abstract class IntroduceMemberAdviceInstance : AdviceInstance
    {
        public Visibility Visibility { get; }

        public bool? IsVirtual { get; }

        public MemberOverrideAction OverrideAction { get; }
    }
}