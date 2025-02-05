// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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