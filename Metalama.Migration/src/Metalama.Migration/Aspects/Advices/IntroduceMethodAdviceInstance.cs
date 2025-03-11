// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using PostSharp.Reflection;
using System;
using System.Reflection;

namespace PostSharp.Aspects.Advices
{
    /// <summary>
    /// In Metalama, call the <c>builder</c>.<see cref="IAspectBuilder.Advice"/>.<see cref="IAdviceFactory.IntroduceMethod"/> method.
    /// </summary>
    /// <seealso href="@introducing-members"/>
    [PublicAPI]
    public sealed class IntroduceMethodAdviceInstance : IntroduceMemberAdviceInstance
    {
        public IntroduceMethodAdviceInstance( MethodInfo method, Visibility visibility, bool? isVirtual, MemberOverrideAction overrideAction )
        {
            throw new NotImplementedException();
        }

        public override object MasterAspectMember { get; }
    }
}