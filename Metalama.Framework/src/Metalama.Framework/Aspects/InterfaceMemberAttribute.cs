// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Advising;
using System;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Custom attribute that, when applied to a member of an aspect class, means that this aspect member is a template implementing a member of an interface implemented by
    /// <see cref="IAdviceFactory.ImplementInterface(Code.INamedType,Code.INamedType,OverrideStrategy,object?)"/>.
    /// </summary>
    [AttributeUsage( AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Event )]
    [PublicAPI]
    public sealed class InterfaceMemberAttribute : TemplateAttribute, IInterfaceMemberAttribute
    {
        /// <summary>
        /// Gets or sets a value indicating whether the interface member should be introduced explicitly.
        /// </summary>
        public bool IsExplicit { get; set; }

        /// <summary>
        /// Gets or sets a value indication the override strategy when interface member conflicts with an existing class member.
        /// </summary>
        public InterfaceMemberOverrideStrategy WhenExists { get; set; }
    }
}