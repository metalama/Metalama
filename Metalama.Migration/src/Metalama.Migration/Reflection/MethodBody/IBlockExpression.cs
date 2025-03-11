// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using PostSharp.Collections;
using System.Collections.Generic;

namespace PostSharp.Reflection.MethodBody
{
    /// <summary>
    /// The source code of method bodies and expression bodies is not represented in the high-level API of Metalama.
    /// If you need to access source code from an aspect, you must implement a service using Metalama SDK. 
    /// </summary>
    /// <seealso href="@roslyn-api"/>
    public interface IBlockExpression : IExpression
    {
        string Label { get; }

        ExceptionHandlerKind ExceptionHandlerKind { get; }

        IExceptionHandler ParentExceptionHandler { get; }

        IReadOnlyLinkedList<IExpression> Items { get; }

        IReadOnlyLinkedList<IExceptionHandler> ExceptionHandlers { get; }

        IList<ILocalVariable> LocalVariables { get; }
    }

    public enum ExceptionHandlerKind
    {
        None,

        Catch,

        Filter,

        Finally
    }
}