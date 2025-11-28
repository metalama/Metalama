// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Invokers;
using System.Collections.Generic;
using System.Reflection;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Represents a method, but not a constructor.
    /// </summary>
    /// <seealso cref="IMethodBase"/>
    /// <seealso cref="IConstructor"/>
    /// <seealso cref="MethodExtensions"/>
    /// <seealso cref="MethodKind"/>
    /// <seealso cref="IMethodInvoker"/>
    /// <seealso cref="MethodAspect"/>
    /// <seealso href="@overriding-methods"/>
    public interface IMethod : IMethodBase, IGeneric, IMethodInvoker
    {
        /// <summary>
        /// Gets the kind of method (such as <see cref="Code.MethodKind.Default"/> or <see cref="Code.MethodKind.PropertyGet"/>.
        /// </summary>
        MethodKind MethodKind { get; }

        /// <summary>
        /// Gets an object representing the method return type and custom attributes, or  <c>null</c> for methods that don't have return types: constructors and finalizers.
        /// </summary>
        IParameter ReturnParameter { get; }

        /// <summary>
        /// Gets the method return type.
        /// </summary>
        IType ReturnType { get; }

        /// <summary>
        /// Gets the base method that is overridden by the current method.
        /// </summary>
        IMethod? OverriddenMethod { get; }

        /// <summary>
        /// Gets a list of interface methods that this method explicitly implements.
        /// </summary>
        IReadOnlyList<IMethod> ExplicitInterfaceImplementations { get; }

        /// <summary>
        /// Gets a <see cref="MethodInfo"/> that represents the current method at run time.
        /// </summary>
        /// <returns>A <see cref="MethodInfo"/> that can be used only in run-time code.</returns>
        /// <seealso href="@reflection"/>
        [CompileTimeReturningRunTime]
        MethodInfo ToMethodInfo();

        /// <summary>
        /// Gets the parent property or event when the current <see cref="IMethod"/> represents a property or event accessor, otherwise <c>null</c>.
        /// </summary>
        IHasAccessors? DeclaringMember { get; }

        /// <summary>
        /// Gets a value indicating whether the method is <c>readonly</c>.
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// Gets a value indicating the type of operator the methods represents.
        /// </summary>
        OperatorKind OperatorKind { get; }

        /// <summary>
        /// Gets the definition of the method. If the current method a generic method instance or a method of
        /// a generic type instance, this returns the generic definition. Otherwise, it returns the current instance.
        /// </summary>
        new IMethod Definition { get; }

        new IRef<IMethod> ToRef();

        /// <summary>
        /// Creates a constructed generic method from the current generic method definition by substituting type arguments.
        /// </summary>
        /// <param name="typeArguments">The type arguments to substitute for the generic method's type parameters.</param>
        /// <returns>A constructed generic method with the specified type arguments.</returns>
        /// <remarks>
        /// This method is analogous to <see cref="System.Reflection.MethodInfo.MakeGenericMethod"/>. The current method must be a generic method definition.
        /// </remarks>
        IMethod MakeGenericInstance( IReadOnlyList<IType> typeArguments );
    }
}