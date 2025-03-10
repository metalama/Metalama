// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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