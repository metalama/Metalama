// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using System;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// An exception thrown by <see cref="IAdviceFactory"/> when compile-time code attempts to add a template
    /// to a target declaration and the template signature is not compatible with the advice and the target declaration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exception is thrown at compile time when an aspect's <see cref="IAspect{T}.BuildAspect"/> method
    /// attempts to apply a T# template whose signature does not match the requirements of the target declaration
    /// or the advice being applied.
    /// </para>
    /// <para>
    /// Common causes include:
    /// </para>
    /// <list type="bullet">
    /// <item><description>The template method's return type is incompatible with the target method's return type.</description></item>
    /// <item><description>The template method's parameters do not match the target method's parameters.</description></item>
    /// <item><description>Using a method template where a property or event template is expected, or vice versa.</description></item>
    /// <item><description>The template is missing required template parameters or has extra unexpected parameters.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IAdviceFactory"/>
    /// <seealso cref="InvalidAdviceParametersException"/>
    /// <seealso cref="TemplateAttribute"/>
    /// <seealso href="@templates"/>
    /// <seealso href="@advising-code"/>
    [CompileTime]
    public sealed class InvalidTemplateSignatureException : Exception
    {
        internal InvalidTemplateSignatureException( string message ) : base( message ) { }
    }
}