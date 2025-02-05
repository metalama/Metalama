// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

// ReSharper disable RedundantUsingDirective

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Engine.Templating
{
    internal enum MetaMemberKind
    {
        /// <summary>
        /// Not a member of the <see cref="meta"/> class.
        /// </summary>
        None,

        /// <summary>
        /// A default member.
        /// </summary>
        Default,

        /// <summary>
        /// The <see cref="meta.InsertComment"/> method.
        /// </summary>
        InsertComment,

        /// <summary>
        /// The <see cref="meta.InsertStatement(IStatement)"/> method (or another overload).
        /// </summary>
        InsertStatement,

        /// <summary>
        /// The <see cref="meta.This"/> property.
        /// </summary>
        This,

        Proceed,

        ProceedAsync,

        ProceedEnumerable,

        ProceedEnumerator,

        ProceedAsyncEnumerable,

        ProceedAsyncEnumerator,

        /// <summary>
        /// The <see cref="meta.InvokeTemplate(TemplateInvocation, object?)" /> method (or its overload).
        /// </summary>
        InvokeTemplate,

        /// <summary>
        /// The <see cref="meta.Return()" /> method (or its overload).
        /// </summary>
        Return,

        DefineLocalVariable,

        RunTime
    }
}