// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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