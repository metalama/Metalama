// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Kinds of <see cref="IDeclaration"/>.
    /// </summary>
    [CompileTime]
    public enum DeclarationKind
    {
        /// <summary>
        /// Not a valid declaration represented by <see cref="IDeclaration"/>.
        /// </summary>
        None,

        /// <summary>
        /// An <see cref="ICompilation"/>.
        /// </summary>
        Compilation,

        /// <summary>
        /// An <see cref="INamedType"/> or an <see cref="ITupleType"/>, but not an <see cref="IExtensionBlock"/>.
        /// </summary>
        NamedType,

        /// <summary>
        /// An <see cref="IMethod"/>, but not a finalizer nor an operator.
        /// </summary>
        Method,

        /// <summary>
        /// An <see cref="IProperty"/>.
        /// </summary>
        Property,

        /// <summary>
        /// An <see cref="IIndexer"/>.
        /// </summary>
        Indexer,

        /// <summary>
        /// An <see cref="IField"/>.
        /// </summary>
        Field,

        /// <summary>
        /// An <see cref="IEvent"/>.
        /// </summary>
        Event,

        /// <summary>
        /// An <see cref="IParameter"/>.
        /// </summary>
        Parameter,

        /// <summary>
        /// An <see cref="ITypeParameter"/>.
        /// </summary>
        TypeParameter,

        /// <summary>
        /// An <see cref="IAttribute"/>.
        /// </summary>
        Attribute,

        /// <summary>
        /// An <see cref="IManagedResource"/>.
        /// </summary>
        ManagedResource,

        /// <summary>
        /// An <see cref="IConstructor"/>.
        /// </summary>
        Constructor,

        /// <summary>
        /// An <see cref="IMethod"/> that is a finalizer (historically referred to as destructors).
        /// </summary>
        Finalizer,

        /// <summary>
        /// An <see cref="IMethod"/> that is an operator.
        /// </summary>
        Operator,

        /// <summary>
        /// A reference assembly, implementing <see cref="IAssembly"/>. Note
        /// that the current assembly is represented by <see cref="ICompilation"/> that inherits <see cref="IAssembly"/>, but the
        /// <see cref="DeclarationKind"/> for the current compilation is <see cref="Compilation"/> and not <see cref="AssemblyReference"/>. 
        /// </summary>
        AssemblyReference,

        /// <summary>
        /// An <see cref="INamespace"/>.
        /// </summary>
        Namespace,

        /// <summary>
        /// An <see cref="IType"/>, but neither an <see cref="INamedType"/>, <see cref="ITypeParameter"/>, <see cref="ITupleType"/>, nor <see cref="IExtensionBlock"/>.
        /// Note that <see cref="IType"/> is not an <see cref="IDeclaration"/>.
        /// </summary>
        Type,

        /// <summary>
        /// An <see cref="IExtensionBlock"/>.
        /// </summary>
        ExtensionBlock
    }
}