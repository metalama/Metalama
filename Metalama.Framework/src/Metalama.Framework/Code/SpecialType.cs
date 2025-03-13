// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

// ReSharper disable InconsistentNaming

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Special types, such as <see cref="Void"/>.
    /// </summary>
    [CompileTime]
    public enum SpecialType
    {
        /// <summary>
        /// Not a special type.
        /// </summary>
        None,

        /// <summary>
        /// <see cref="Void" />.
        /// </summary>
        Void,

        /// <summary>
        /// <see cref="object" />.
        /// </summary>
        Object,

        /// <summary>
        /// <see cref="byte" />.
        /// </summary>
        Byte,

        /// <summary>
        /// <see cref="sbyte" />.
        /// </summary>
        SByte,

        /// <summary>
        /// <see cref="short" />.
        /// </summary>
        Int16,

        /// <summary>
        /// <see cref="ushort" />.
        /// </summary>
        UInt16,

        /// <summary>
        /// <see cref="int" />.
        /// </summary>
        Int32,

        /// <summary>
        /// <see cref="uint" />.
        /// </summary>
        UInt32,

        /// <summary>
        /// <see cref="long" />.
        /// </summary>
        Int64,

        /// <summary>
        /// <see cref="ulong" />.
        /// </summary>
        UInt64,

        /// <summary>
        /// <see cref="string" />.
        /// </summary>
        String,

        /// <summary>
        /// <see cref="decimal" />.
        /// </summary>
        Decimal,

        /// <summary>
        /// <see cref="float" />.
        /// </summary>
        Single,

        /// <summary>
        /// <see cref="double" />.
        /// </summary>
        Double,

        /// <summary>
        /// <see cref="bool" />.
        /// </summary>
        Boolean,

        /// <summary>
        /// <see cref="System.Collections.IEnumerable" />.
        /// </summary>
        IEnumerable,

        /// <summary>
        /// <see cref="System.Collections.IEnumerator" />.
        /// </summary>
        IEnumerator,

        /// <summary>
        /// <see cref="System.Collections.Generic.IEnumerable{T}" />.
        /// </summary>
        IEnumerable_T,

        /// <summary>
        /// <see cref="System.Collections.Generic.IEnumerator{T}" />.
        /// </summary>
        IEnumerator_T,

        /// <summary>
        /// <see cref="System.Collections.Generic.List{T}" />.
        /// </summary>
        List_T,

        /// <summary>
        /// <c>System.Collections.Generic.IAsyncEnumerable&gt;T&lt;</c>.
        /// </summary>
        IAsyncEnumerable_T,

        /// <summary>
        /// <c>System.Collections.Generic.IAsyncEnumerator&gt;T&lt;</c>.
        /// </summary>
        IAsyncEnumerator_T,

        /// <summary>
        /// <c>System.Threading.Tasks.ValueTask</c>.
        /// </summary>
        ValueTask,

        /// <summary>
        /// <c>System.Threading.Tasks.ValueTask&gt;T&lt;</c>.
        /// </summary>
        ValueTask_T,

        /// <summary>
        /// <see cref="System.Threading.Tasks.Task" />.
        /// </summary>
        Task,

        /// <summary>
        /// <see cref="System.Threading.Tasks.Task{T}" />.
        /// </summary>
        Task_T,

        /// <summary>
        /// <see cref="char" />.
        /// </summary>
        Char,

        /// <summary>
        /// <see cref="System.Type" />.
        /// </summary>
        Type,

        // Must be last.

        /// <summary>
        /// Number of items in this enumeration.
        /// </summary>
        Count
    }
}