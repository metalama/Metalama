// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Base interface for <see cref="IMethod"/>, <see cref="IFieldOrProperty"/>, <see cref="IEvent"/>, but not <see cref="INamedType"/>.
    /// </summary>
    public interface IMember : IMemberOrNamedType
    {
        /// <summary>
        /// Gets a value indicating whether the member is <c>virtual</c>.
        /// </summary>
        /// <seealso cref="MemberExtensions.IsOverridable"/>
        bool IsVirtual { get; }

        /// <summary>
        /// Gets a value indicating whether the member is <c>async</c>.
        /// </summary>
        bool IsAsync { get; }

        /// <summary>
        /// Gets a value indicating whether the member is <c>override</c>.
        /// </summary>
        /// <seealso cref="MemberExtensions.IsOverridable"/>
        bool IsOverride { get; }

        /// <summary>
        /// Gets a value indicating whether the member has an external implementation, i.e. has the <c>extern</c> modifier.
        /// </summary>
        bool IsExtern { get; }

        /// <summary>
        /// Gets a value indicating whether the member is an explicit implementation of an interface member.
        /// </summary>
        bool IsExplicitInterfaceImplementation { get; }

        /// <summary>
        /// Gets a value indicating whether the member has an implementation or is only a definition without a body.
        /// </summary>
        /// <remarks>
        /// Members without implementation are, for example, <c>abstract</c> members, <c>extern</c> methods, <c>partial</c> members without implementation part or <c>const</c> fields. 
        /// </remarks>
        bool HasImplementation { get; }

        /// <summary>
        /// Gets the type containing the current member.
        /// </summary>
        new INamedType DeclaringType { get; }

        /// <summary>
        /// Gets the definition of the member. If the current declaration is a generic method instance or a member of
        /// a generic type instance, this returns the generic definition. Otherwise, it returns the current instance.
        /// </summary>
        new IMember Definition { get; }

        new IRef<IMember> ToRef();
        
        IMember? OverriddenMember { get; }
    }
}