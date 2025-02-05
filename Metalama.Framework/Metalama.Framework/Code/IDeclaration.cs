// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Metrics;
using Metalama.Framework.Utilities;
using System;
using System.Collections.Immutable;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Represent a declaration.
    /// </summary>
    /// <remarks>
    /// The <see cref="IDeclaration"/> interface implements <see cref="IEquatable{T}"/>. The implementation uses the <see cref="ICompilationComparers.Default"/> comparer.
    /// To use a different comparer, choose a different comparer from <see cref="IDeclaration"/>.<see cref="ICompilationElement.Compilation"/>.<see cref="ICompilation.Comparers"/>.
    /// </remarks>
    /// <seealso cref="DeclarationExtensions"/>
    [CompileTime]
    public interface IDeclaration : IDisplayable, IDiagnosticLocation, ICompilationElement, IMeasurable, IEquatable<IDeclaration>
    {
        /// <summary>
        /// Gets a reference to the compilation, which can be used to identify the current declaration
        /// in a different revision of the compilation. The reference object is compile-time serializable. It is guaranteed to
        /// be deserializable in a different process, even with a different version of Metalama.
        /// </summary>
        /// <returns></returns>
        IRef<IDeclaration> ToRef();

        /// <summary>
        /// Gets a serializable identifier for the current declaration. This identifier is guaranteed to
        /// be deserializable in a different process, even with a different version of Metalama.
        /// </summary>
        /// <seealso cref="ToRef"/>
        SerializableDeclarationId ToSerializableId();

        /// <summary>
        /// Gets the declaring assembly, which can be the current <see cref="ICompilationElement.Compilation"/>
        /// or a reference assembly.
        /// </summary>
        IAssembly DeclaringAssembly { get; }

        /// <summary>
        /// Gets the origin of the current declaration.
        /// </summary>
        IDeclarationOrigin Origin { get; }

        /// <summary>
        /// Gets the containing declaration, such as a <see cref="INamedType"/> for nested
        /// types or for methods. For non-nested types, returns the containing assembly
        /// (and not the namespace, use <see cref="INamedType.ContainingNamespace"/> for that).
        /// </summary>
        IDeclaration? ContainingDeclaration { get; }

        /// <summary>
        /// Gets the collection of custom attributes on the declaration.
        /// </summary>
        IAttributeCollection Attributes { get; }

        /// <summary>
        /// Gets a value indicating whether the member is implicitly declared, i.e. declared without being represented in source code.
        /// Returns <c>false</c> if it is explicitly declared in code.
        /// </summary>
        bool IsImplicitlyDeclared { get; }

        /// <summary>
        /// Gets the depth of the current declaration in the code model. The value of the <see cref="Depth"/> property has no absolute meaning,
        /// only a relative one, i.e. it is only relevant when comparing the depth of two declarations. A declaration has always a greater depth
        /// than the declaration in which it is contained. A type has always a greater depths than the base it derives from or the interfaces
        /// it implements.
        /// </summary>
        [Hidden]
        int Depth { get; }

        /// <summary>
        /// Gets a value indicating whether the current declaration is declared to the current project. It returns <c>false</c> for declarations
        /// declared in referenced projects or assemblies.
        /// </summary>
        bool BelongsToCurrentProject { get; }

        /// <summary>
        /// Gets the set of syntax nodes of the source code that declare the current declaration, or an empty
        /// set if the current declaration is not backed by source code.
        /// </summary>
        ImmutableArray<SourceReference> Sources { get; }

        /// <summary>
        /// Gets the <see cref="IGenericContext"/> for the current declaration.
        /// </summary>
        IGenericContext GenericContext { get; }
    }
}