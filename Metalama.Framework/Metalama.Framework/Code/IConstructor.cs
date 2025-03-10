// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code.Invokers;
using System.Reflection;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Represents an instance constructor or a static constructor.
    /// </summary>
    public interface IConstructor : IMethodBase, IConstructorInvoker
    {
        /// <summary>
        /// Gets a value indicating whether this constructor is the primary constructor of the type.
        /// </summary>
        /// <remarks>
        /// Primary constructors are recognized only for the current compilation.
        /// </remarks>
        bool IsPrimary { get; }

        /// <summary>
        /// Gets a <see cref="ConstructorInitializerKind" /> that specifies the initializer semantics of the constructor.
        /// </summary>
        public ConstructorInitializerKind InitializerKind { get; }

        /// <summary>
        /// Gets a <see cref="ConstructorInfo"/> that represents the current constructor at run time.
        /// </summary>
        /// <returns>A <see cref="ConstructorInfo"/> that can be used only in run-time code.</returns>
        ConstructorInfo ToConstructorInfo();

        /// <summary>
        /// Gets the definition of the constructor. If the current declaration is a constructor of
        /// a generic type instance, this returns the constructor in the generic type definition. Otherwise, it returns the current instance.
        /// </summary>
        new IConstructor Definition { get; }

        new IRef<IConstructor> ToRef();
    }
}