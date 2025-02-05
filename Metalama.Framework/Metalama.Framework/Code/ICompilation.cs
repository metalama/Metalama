// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Project;
using Metalama.Framework.Utilities;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Represents a set of types compiled together. See also <see cref="IProject"/>.
    /// </summary>
    [CompileTime]
    public interface ICompilation : IAssembly
    {
        /// <summary>
        /// Gets information about the project from which the compilation was created.
        /// </summary>
        IProject Project { get; }

        /// <summary>
        /// Gets the list of managed resources in the current compilation.
        /// </summary>
        [Obsolete( "Not implemented." )]
        IReadOnlyList<IManagedResource> ManagedResources { get; }

        /// <summary>
        /// Gets a equality comparers that can be used with declarations of this compilation.
        /// </summary>
        [Hidden]
        ICompilationComparers Comparers { get; }

        /// <summary>
        /// Gets the set of types, in the current compilation, that are derived from a given base type (given as an <see cref="INamedType"/>).
        /// </summary>
        /// <param name="baseType">The base type.</param>
        /// <param name="options">Options that determine whether the search should be deep or shallow.</param>
        IEnumerable<INamedType> GetDerivedTypes( INamedType baseType, DerivedTypesOptions options = default );

        /// <summary>
        /// Gets the set of types, in the current compilation, that are derived from a given base type (given as a <see cref="Type"/>).
        /// </summary>
        /// <param name="baseType">The base type.</param>
        /// <param name="options">Options that determine whether the search should be deep or shallow.</param>
        IEnumerable<INamedType> GetDerivedTypes( Type baseType, DerivedTypesOptions options = default );

        /// <summary>
        /// Gets all attributes of a given type in the current compilation, where the attribute type is given as an <see cref="INamedType"/>.
        /// </summary>
        /// <param name="type">The attribute type.</param>
        /// <param name="includeDerivedTypes">A value indicating whether attributes of types derived from <paramref name="type"/> should be returned as well.</param>
        /// <returns>A list of attributes.</returns>
        IEnumerable<IAttribute> GetAllAttributesOfType( INamedType type, bool includeDerivedTypes = false );

        /// <summary>
        /// Gets all attributes of a given type in the current compilation, where the attribute type is given as a <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The attribute type.</param>
        /// <param name="includeDerivedTypes">A value indicating whether attributes of types derived from <paramref name="type"/> should be returned as well.</param>
        /// <returns>A list of attributes.</returns>
        IEnumerable<IAttribute> GetAllAttributesOfType( Type type, bool includeDerivedTypes = false );

        /// <summary>
        /// Gets a value indicating whether the current compilation is partial, i.e. incomplete. Metalama uses partial compilations
        /// at design time, when only the closure of modified types are being incrementally recompiled.
        /// </summary>
        bool IsPartial { get; }

        /// <summary>
        /// Gets a service that caches declarations of the current compilation. It is typically used to cache often-used declarations
        /// across aspect instances.
        /// </summary>
        [Hidden]
        IDeclarationCache Cache { get; }

        new IRef<ICompilation> ToRef();

        /// <summary>
        /// Gets a service that allows to create declaration and type instances for the current <see cref="ICompilation"/>.
        /// </summary>
        IDeclarationFactory Factory { get; }
    }
}