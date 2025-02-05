// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using System.Reflection;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Base interface for <see cref="IMethod"/>, <see cref="IFieldOrPropertyOrIndexer"/>, <see cref="IEvent"/>, and <see cref="INamedType"/>.
    /// </summary>
    public interface IMemberOrNamedType : INamedDeclaration
    {
        /// <summary>
        /// Gets the member accessibility (or visibility), i.e. <see cref="Code.Accessibility.Private"/>, <see cref="Code.Accessibility.Protected"/>
        /// and so on.
        /// </summary>
        Accessibility Accessibility { get; }

        bool IsAbstract { get; }

        /// <summary>
        /// Gets a value indicating whether the member is <c>static</c>.
        /// </summary>
        bool IsStatic { get; }

        /// <summary>
        /// Gets a value indicating whether the member is <c>sealed</c>.
        /// </summary>
        /// <seealso cref="MemberExtensions.IsOverridable"/>
        bool IsSealed { get; }

        /// <summary>
        /// Gets a value indicating whether the member hides another member defined in base types. When <c>true</c>, this is equivalent to <c>new</c> keyword being <i>expected</i> in the source code although this property will return <c>true</c> even when the <c>new</c> keyword is not present but expected.
        /// </summary>
        bool IsNew { get; }

        /// <summary>
        /// Gets the type containing the current member, or <c>null</c> if the current member is not contained
        /// within a type.
        /// </summary>
        INamedType? DeclaringType { get; }

        /// <summary>
        /// Gets a <see cref="MemberInfo"/> that represents the current member at run time.
        /// </summary>
        /// <returns>A <see cref="MemberInfo"/> that can be used only in run-time code.</returns>
        [CompileTimeReturningRunTime]
        MemberInfo ToMemberInfo();

        /// <summary>
        /// Gets a value indicating whether the declaration executes at compile time, at run time, or both.
        /// </summary>
        /// <seealso cref="CompileTimeAttribute"/>
        /// <seealso cref="RunTimeOrCompileTimeAttribute"/>
        ExecutionScope ExecutionScope { get; }

        /// <summary>
        /// Gets the definition of the member or type. If the current declaration is a generic type instance, a generic method instance, or a member of
        /// a generic type instance, this returns the generic definition. Otherwise, it returns the current instance.
        /// </summary>
        IMemberOrNamedType Definition { get; }

        new IRef<IMemberOrNamedType> ToRef();

        /// <summary>
        /// Gets a value indicating whether the type or member is marked as <c>partial</c> in source code. 
        /// </summary>
        /// <remarks>
        /// To check whether a partial member has an implementation, check the <see cref="IMember.HasImplementation"/> property.
        /// </remarks>
        bool IsPartial { get; }
    }
}