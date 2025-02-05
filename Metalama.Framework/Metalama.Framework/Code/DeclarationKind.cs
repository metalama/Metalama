// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
        /// <see cref="ICompilation"/>.
        /// </summary>
        Compilation,

        /// <summary>
        /// <see cref="INamedType"/>.
        /// </summary>
        NamedType,

        /// <summary>
        /// <see cref="IMethod"/>.
        /// </summary>
        Method,

        /// <summary>
        /// <see cref="IProperty"/>.
        /// </summary>
        Property,

        /// <summary>
        /// <see cref="IIndexer"/>.
        /// </summary>
        Indexer,

        /// <summary>
        /// <see cref="IField"/>.
        /// </summary>
        Field,

        /// <summary>
        /// <see cref="IEvent"/>.
        /// </summary>
        Event,

        /// <summary>
        /// <see cref="IParameter"/>.
        /// </summary>
        Parameter,

        /// <summary>
        /// <see cref="ITypeParameter"/>.
        /// </summary>
        TypeParameter,

        /// <summary>
        /// <see cref="IAttribute"/>.
        /// </summary>
        Attribute,

        /// <summary>
        /// <see cref="IManagedResource"/>.
        /// </summary>
        ManagedResource,

        /// <summary>
        /// <see cref="IConstructor"/>.
        /// </summary>
        Constructor,

        /// <summary>
        /// <see cref="IMethod"/> that is a finalizer (historically referred to as destructors).
        /// </summary>
        Finalizer,

        /// <summary>
        /// <see cref="IMethod"/> that is an operator.
        /// </summary>
        Operator,

        /// <summary>
        /// A reference assembly, implementing <see cref="IAssembly"/>. Note
        /// that the current assembly is represented by <see cref="ICompilation"/> that inherits <see cref="IAssembly"/>, but the
        /// <see cref="DeclarationKind"/> for the current compilation is <see cref="Compilation"/> and not <see cref="AssemblyReference"/>. 
        /// </summary>
        AssemblyReference,

        /// <summary>
        /// <see cref="INamespace"/>.
        /// </summary>
        Namespace,

        /// <summary>
        /// <see cref="IType"/>, but neither an <see cref="INamedType"/> nor an <see cref="ITypeParameter"/>.
        /// Note that <see cref="IType"/> is not an <see cref="IDeclaration"/>.
        /// </summary>
        Type
    }
}