// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Engine.CompileTime
{
    /// <summary>
    /// Kinds of template members as specified by their attribute.
    /// </summary>
    internal enum TemplateAttributeType
    {
        // WARNING! Values are Json-serialized, so they cannot changed.

        /// <summary>
        /// Not a template.
        /// </summary>
        None,

        /// <summary>
        /// Template for programmatic advice.
        /// </summary>
        Template,

        /// <summary>
        /// A declarative advice, which derives from <see cref="DeclarativeAdviceAttribute"/>.
        /// </summary>
        DeclarativeAdvice,

        /// <summary>
        /// Interface member.
        /// </summary>
        InterfaceMember
    }
}