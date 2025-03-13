// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.Templating
{
    /// <summary>
    /// Specified semantics the <see cref="TemplateCompiler" /> should compile the template with.
    /// </summary>
    internal enum TemplateCompilerSemantics
    {
        /// <summary>
        /// The template should be compiled with default semantics.
        /// </summary>
        Default,

        /// <summary>
        /// The template should be compiled with initializer semantics.
        /// </summary>
        Initializer
    }
}