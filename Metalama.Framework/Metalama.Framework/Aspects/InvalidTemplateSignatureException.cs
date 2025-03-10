// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using System;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// An exception thrown by <see cref="IAdviceFactory"/> when compile-time code attempts to add a template
    /// to a target declaration and the template is not compatible with the advice and the target declaration.
    /// </summary>
    [CompileTime]
    public sealed class InvalidTemplateSignatureException : Exception
    {
        internal InvalidTemplateSignatureException( string message ) : base( message ) { }
    }
}