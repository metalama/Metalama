// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// ReSharper disable InconsistentNaming

using Metalama.Framework.Advising;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Enumeration of the kind of templates that were specified by the user using <see cref="GetterTemplateSelector"/> or
    /// <see cref="MethodTemplateSelector"/>. A <see cref="MethodTemplateSelector"/> represents the intention of the user, not
    /// a characteristic of the declaration used as a template.
    /// </summary>
    internal enum TemplateKind
    {
        /// <summary>
        /// Not a template.
        /// </summary>
        None,

        /// <summary>
        /// Default template.
        /// </summary>
        Default,

        /// <summary>
        /// <see cref="MethodTemplateSelector.AsyncTemplate"/>.
        /// </summary>
        Async,

        /// <summary>
        /// <see cref="MethodTemplateSelector.EnumerableTemplate"/> or <see cref="GetterTemplateSelector.EnumerableTemplate"/>.
        /// </summary>
        IEnumerable,

        /// <summary>
        /// <see cref="MethodTemplateSelector.EnumeratorTemplate"/> or <see cref="GetterTemplateSelector.EnumeratorTemplate"/>.
        /// </summary>
        IEnumerator,

        /// <summary>
        /// <see cref="MethodTemplateSelector.AsyncEnumerableTemplate"/>.
        /// </summary>
        IAsyncEnumerable,

        /// <summary>
        /// <see cref="MethodTemplateSelector.AsyncEnumeratorTemplate"/>.
        /// </summary>
        IAsyncEnumerator,

        /// <summary>
        /// Template used for an introduction. Can be any type, must be detected from the signature and implementation.
        /// </summary>
        Introduction,

        /// <summary>
        /// Template used for initializer of introduced field, property of event field.
        /// </summary>
        InitializerExpression
    }
}