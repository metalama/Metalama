// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code.Collections;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Represents an assembly (typically a reference assembly).
    /// </summary>
    public interface IAssembly : IDeclaration
    {
        /// <summary>
        /// Gets the global namespace (i.e. the one with an empty name).
        /// </summary>
        INamespace GlobalNamespace { get; }

        /// <summary>
        /// Gets a value indicating whether the assembly represents a reference (<c>true</c>), or the project being built (<c>false</c>).
        /// </summary>
        bool IsExternal { get; }

        /// <summary>
        /// Gets the assembly identity.
        /// </summary>
        IAssemblyIdentity Identity { get; }

        /// <summary>
        /// Gets the list of types declared in this assembly, in all namespaces, but not the nested types.
        /// In case of partial compilations (see <see cref="ICompilation.IsPartial"/>), this collection only contain the types in the current
        /// partial compilation.
        /// </summary>
        INamedTypeCollection Types { get; }

        /// <summary>
        /// Gets the list of types declared in this assembly, in all namespaces, including recursively all nested types.
        /// In case of partial compilations (see <see cref="ICompilation.IsPartial"/>), this collection only contain the types in the current
        /// partial compilation.
        /// </summary>
        INamedTypeCollection AllTypes { get; }

        /// <summary>
        /// Gets a value indicating whether <c>internal</c> members of the current assembly are accessible from a given assembly. 
        /// </summary>
        bool AreInternalsVisibleFrom( IAssembly assembly );

        IAssemblyCollection ReferencedAssemblies { get; }

        new IRef<IAssembly> ToRef();
    }
}