// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace Metalama.Framework.Utilities
{
    /// <summary>
    /// Declares that a type, a field, a property or a parameter is <i>durable</i>, that is, that it is safe to be held
    /// across compilations. An analyzer verifies the declaration and reports a warning when it does not hold.
    /// </summary>
    /// <remarks>
    /// <para>
    /// At design time the analysis process lives for as long as the solution is open, and Roslyn produces a new
    /// compilation on essentially every keystroke. An object that outlives a single request must therefore not hold a
    /// strong reference to a compilation, a syntax tree, a semantic model, a symbol, a code model declaration or a
    /// non-durable <see cref="Metalama.Framework.Code.IRef{T}"/>, nor to anything that transitively reaches one. A
    /// single retained compilation pins every syntax tree of the project and the symbol tables built from it.
    /// </para>
    /// <para>
    /// The meaning of this attribute depends on what it is applied to.
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// On a <b>type</b>, it means that every instance field and automatically implemented property of that type is
    /// durable. For a collection, the type arguments must be durable in turn.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// On a <b>field</b> or an automatically implemented <b>property</b> whose declared type is not durable, it waives
    /// the check on the declared type and requires instead that every value assigned to that member have a durable
    /// type. This is the form to use for a member whose declared type is an interface, <see cref="object"/>, or a
    /// delegate. A delegate type is never durable, because a delegate holds its target and everything its closure
    /// captured, but an individual delegate may be: a static method group captures nothing, and a lambda is analyzed
    /// for what it actually captures, so the assignment carries evidence the declared type cannot.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// A member marked in this way must not be writable from outside the type that declares it, that is, it must be
    /// read-only or private, or, for a property, have a private setter. The waiver replaces a check on the declared
    /// type with a check on the assignments, so it holds only where every assignment can be seen.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// On a <b>parameter</b>, it means that every argument passed at every call site must have a durable type. When
    /// the argument is a lambda expression, the variables that the lambda captures are analyzed, because a lambda
    /// holds its closure and whatever holds the delegate holds everything the closure captured.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// Intrinsic types are durable, and a system collection is durable when its type arguments are. A type that is
    /// not marked with this attribute and is not otherwise known to the analyzer is not durable, so marking a type
    /// propagates the obligation to the types of all of its members.
    /// </para>
    /// <para>
    /// Where durability holds but cannot be established by the analyzer, use
    /// <see cref="DurableDangerous{T}"/>. Where a value must be computed lazily, use <see cref="DurableLazy{T}"/>
    /// rather than <see cref="System.Lazy{T}"/>, which holds its factory delegate and therefore that delegate's
    /// closure.
    /// </para>
    /// </remarks>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface
        | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter,
        Inherited = false )]
    [PublicAPI]
    public sealed class DurableAttribute : Attribute;
}
