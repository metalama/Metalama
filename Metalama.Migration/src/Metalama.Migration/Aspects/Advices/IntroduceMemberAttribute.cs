// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using PostSharp.Reflection;
using System;

namespace PostSharp.Aspects.Advices
{
    /// <summary>
    /// In Metalama, use the <see cref="IntroduceAttribute"/> custom attribute. 
    /// </summary>
    /// <seealso href="@introducing-members"/>
    [AttributeUsage( AttributeTargets.Event | AttributeTargets.Property | AttributeTargets.Method )]
    [PublicAPI]
    public sealed class IntroduceMemberAttribute : Advice
    {
        public Visibility Visibility { get; set; }

        public bool IsVirtual { get; set; }

        public bool IsIsVirtualSpecified { get; }

        public MemberOverrideAction OverrideAction { get; set; }
    }
}