// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Types;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Kinds of <see cref="IType"/>.
    /// </summary>
    [CompileTime]
    public enum TypeKind
    {
        /// <summary>
        /// Array (<see cref="IArrayType"/>).
        /// </summary>
        Array,

        /// <summary>
        /// <c>class</c> (<see cref="INamedType"/>).
        /// </summary>
        /// <see cref="RecordClass"/>
        Class,

        /// <summary>
        /// <c>record class</c> (i.e. <c>record</c>) (<see cref="INamedType"/>).
        /// </summary>
        RecordClass,

        /// <summary>
        /// <c>delegate</c> (<see cref="INamedType"/>).
        /// </summary>
        Delegate,

        /// <summary>
        /// <c>dynamic</c> (<see cref="IDynamicType"/>).
        /// </summary>
        Dynamic,

        /// <summary>
        /// <c>enum</c> (<see cref="INamedType"/>).
        /// </summary>
        Enum,

        /// <summary>
        /// Generic parameter (<see cref="ITypeParameter"/>).
        /// </summary>
        TypeParameter,

        /// <summary>
        /// <c>interface</c> (<see cref="INamedType"/>).
        /// </summary>
        Interface,

        /// <summary>
        /// Unmanaged pointer (<c>*</c>) (<see cref="IPointerType"/>).
        /// </summary>
        Pointer,

        /// <summary>
        /// <c>struct</c>.
        /// </summary>
        /// <seealso cref="RecordStruct"/> (<see cref="INamedType"/>).
        Struct,

        /// <summary>
        /// <c>record struct</c> (<see cref="INamedType"/>).
        /// </summary>
        RecordStruct,

        /// <summary>
        /// Function pointer (<c>delegate*</c>) (<see cref="IFunctionPointerType"/>).
        /// </summary>
        FunctionPointer,

        /// <summary>
        /// At design time, a type that does not exist.
        /// </summary>
        Error,

        /// <summary>
        /// An extension block (<see cref="IExtensionBlock"/>).
        /// </summary>
        Extension,

        /// <summary>
        /// A tuple (<see cref="ITupleType"/>).
        /// </summary>
        Tuple
    }
}