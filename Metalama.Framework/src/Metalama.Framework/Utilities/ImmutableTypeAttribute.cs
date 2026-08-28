// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace Metalama.Framework.Utilities
{
    /// <summary>
    /// Declares that a type must be written in an immutable style, and requires every type that derives from it or
    /// implements it to be written that way too. An analyzer verifies the declaration and reports a warning when it
    /// does not hold. Apply <c>[ImmutableType( false )]</c> to waive the requirement on one type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An aspect is instantiated once and reused for every target it applies to, and at design time across
    /// compilations. State kept on it therefore leaks from one target to the next, in an order the author does not
    /// control. That is why <see cref="Metalama.Framework.Aspects.IAspect"/> and
    /// <see cref="Metalama.Framework.Fabrics.Fabric"/> carry this attribute, and why every aspect, fabric and
    /// validator is checked.
    /// </para>
    /// <para>
    /// Concretely, in a type subject to this contract:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// every instance <b>field</b> must be <c>readonly</c>, or private and assigned only in a constructor or an
    /// <c>init</c> accessor;
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// every automatically implemented <b>property</b> must have no setter, an <c>init</c> accessor, or a private
    /// setter assigned only in a constructor or an <c>init</c> accessor;
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// and the <b>type</b> of every such member must itself be immutable, all the way down.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// A member whose write access is private is checked at its assignments rather than at its declaration, because
    /// private write access confines every assignment to the declaring type, so the analyzer can see all of them.
    /// Passing such a member as a <c>ref</c> or <c>out</c> argument counts as an assignment.
    /// </para>
    /// <para>
    /// Intrinsic types, delegates, enumerations and the immutable collections are immutable, and an immutable
    /// collection is immutable only when its type arguments are. A type that is not marked with this attribute and is
    /// not otherwise known to the analyzer is not immutable, so marking a type propagates the obligation to the types
    /// of all of its members.
    /// </para>
    /// <para>
    /// <b>This attribute is deliberately not <c>System.ComponentModel.ImmutableObjectAttribute</c>.</b> That one
    /// exists to tell a designer that an object has no editable sub-properties, it is applied in the wild for that
    /// reason, and it says nothing about this contract. Reusing it would mean checking code whose author never opted
    /// in. The name also avoids <c>Metalama.Patterns.Immutability.ImmutableAttribute</c>, which is a different feature
    /// and may be imported in the same file.
    /// </para>
    /// <para>
    /// A project that implements the framework itself, rather than using it, sets the
    /// <c>MetalamaEnforceImmutabilityContract</c> MSBuild property to <c>false</c> to turn the contract off entirely.
    /// </para>
    /// </remarks>
    /// <seealso cref="DurableAttribute"/>
    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = false )]
    [PublicAPI]
    public sealed class ImmutableTypeAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableTypeAttribute"/> class.
        /// </summary>
        /// <param name="isImmutable">
        /// <c>true</c> to require the type to be written in an immutable style, which is the default; <c>false</c> to
        /// waive that requirement on a type that would otherwise inherit it. The waiver is greppable and appears in
        /// the declaration, which a <c>#pragma</c> does not, and the analyzer reports it so that a review can find
        /// every one of them.
        /// </param>
        public ImmutableTypeAttribute( bool isImmutable = true )
        {
            this.IsImmutable = isImmutable;
        }

        /// <summary>
        /// Gets a value indicating whether the type must be written in an immutable style.
        /// </summary>
        public bool IsImmutable { get; }
    }
}
