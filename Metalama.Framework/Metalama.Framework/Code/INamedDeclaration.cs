// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Code
{
    public interface INamedDeclaration : IDeclaration
    {
        /// <summary>
        /// Gets the declaration name. If the member is an <see cref="INamedType"/> or <see cref="INamespace"/>, the <see cref="Name"/>
        /// property gets the short name of the type or namespace, without the parent namespace. See also <see cref="INamedType.ContainingNamespace"/>
        /// and <see cref="INamespaceOrNamedType.FullName"/>.
        /// </summary>
        string Name { get; }
    }
}