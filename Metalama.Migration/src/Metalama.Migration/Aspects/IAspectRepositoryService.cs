// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using PostSharp.Extensibility;
using System;

namespace PostSharp.Aspects
{
    /// <summary>
    /// In Metalama, use extension methods of the code model.
    /// </summary>
    public interface IAspectRepositoryService : IService
    {
        /// <summary>
        /// Use <see cref="IDeclaration"/>.<see cref="DeclarationExtensions.Enhancements{T}"/>.
        /// </summary>
        IAspectInstance[] GetAspectInstances( object declaration );

        /// <summary>
        /// Use <see cref="IDeclaration"/>.<see cref="DeclarationExtensions.Enhancements{T}"/>.<c>Any()</c>.
        /// </summary>
        bool HasAspect( object declaration, Type aspectType );

        /// <summary>
        /// This event is not exposed, but when you register validators, they get executed
        /// after all aspects have been applied.
        /// </summary>
        /// <seealso href="@aspect-validating"/>
        [Obsolete( "", true )]
        event EventHandler AspectDiscoveryCompleted;
    }
}