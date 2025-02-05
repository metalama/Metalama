// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Kinds of <see cref="IType"/>.
    /// </summary>
    [CompileTime]
    public enum TypeKind
    {
        /// <summary>
        /// Array.
        /// </summary>
        Array,

        /// <summary>
        /// <c>class</c>.
        /// </summary>
        /// <see cref="RecordClass"/>
        Class,

        /// <summary>
        /// <c>record class</c> (i.e. <c>record</c>).
        /// </summary>
        RecordClass,

        /// <summary>
        /// <c>delegate</c>.
        /// </summary>
        Delegate,

        /// <summary>
        /// <c>dynamic</c>.
        /// </summary>
        Dynamic,

        /// <summary>
        /// <c>enum</c>.
        /// </summary>
        Enum,

        /// <summary>
        /// Generic parameter.
        /// </summary>
        TypeParameter,

        /// <summary>
        /// <c>interface</c>.
        /// </summary>
        Interface,

        /// <summary>
        /// Unmanaged pointer (<c>*</c>).
        /// </summary>
        Pointer,

        /// <summary>
        /// <c>struct</c>.
        /// </summary>
        /// <seealso cref="RecordStruct"/>
        Struct,

        /// <summary>
        /// <c>record struct</c>.
        /// </summary>
        RecordStruct,

        /// <summary>
        /// Function pointer (<c>delegate*</c>).
        /// </summary>
        FunctionPointer,

        /// <summary>
        /// At design time, a type that does not exist.
        /// </summary>
        Error
    }
}