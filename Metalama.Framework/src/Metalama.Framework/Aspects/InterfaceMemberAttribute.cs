// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Marks a member in an aspect class as a template for implementing an interface member.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this attribute on methods, properties, fields, or events in your aspect class that should implement
    /// members of an interface being added via <see cref="AdviserExtensions.ImplementInterface(IAdviser{Code.INamedType}, Code.INamedType, OverrideStrategy, object?)"/>.
    /// </para>
    /// <para>
    /// Unlike <see cref="IntroduceAttribute"/>, members marked with <c>[InterfaceMember]</c> are only introduced when
    /// the corresponding <see cref="AdviserExtensions.ImplementInterface(IAdviser{Code.INamedType}, Code.INamedType, OverrideStrategy, object?)"/> advice succeeds.
    /// If the advice is ignored (because the interface was already implemented and <see cref="OverrideStrategy.Ignore"/> was used),
    /// the member is not introduced.
    /// </para>
    /// <para>
    /// By default, an implicit (public) implementation is created. Set <see cref="IsExplicit"/> to <c>true</c> to create
    /// an explicit interface implementation instead.
    /// </para>
    /// </remarks>
    /// <seealso cref="AdviserExtensions.ImplementInterface(IAdviser{Code.INamedType}, Code.INamedType, OverrideStrategy, object?)"/>
    /// <seealso cref="InterfaceMemberOverrideStrategy"/>
    /// <seealso cref="IntroduceAttribute"/>
    /// <seealso href="@implementing-interfaces"/>
    [AttributeUsage( AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Event )]
    [PublicAPI]
    public sealed class InterfaceMemberAttribute : TemplateAttribute, IInterfaceMemberAttribute
    {
        /// <summary>
        /// Gets or sets a value indicating whether the interface member should be introduced as an explicit implementation.
        /// </summary>
        /// <remarks>
        /// When <c>true</c>, the member is introduced as an explicit interface implementation (e.g., <c>void IDisposable.Dispose()</c>).
        /// When <c>false</c> (the default), an implicit public implementation is created.
        /// </remarks>
        public bool IsExplicit { get; set; }

        /// <summary>
        /// Gets or sets the strategy to use when an interface member conflicts with an existing class member.
        /// </summary>
        /// <remarks>
        /// The default is <see cref="InterfaceMemberOverrideStrategy.Default"/>, which inherits the behavior from the
        /// <see cref="OverrideStrategy"/> parameter passed to
        /// <see cref="AdviserExtensions.ImplementInterface(IAdviser{Code.INamedType}, Code.INamedType, OverrideStrategy, object?)"/>.
        /// </remarks>
        public InterfaceMemberOverrideStrategy WhenExists { get; set; }
    }
}