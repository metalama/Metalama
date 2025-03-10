// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Kinds of <see cref="AspectPredecessor"/>.
    /// </summary>
    [CompileTime]
    public enum AspectPredecessorKind
    {
        /// <summary>
        /// The aspect has been created by a custom attribute. <see cref="AspectPredecessor.Instance"/> is an <see cref="IAttribute"/>.
        /// </summary>
        Attribute,

        /// <summary>
        /// The aspect has been created by another aspect. <see cref="AspectPredecessor.Instance"/> is an <see cref="IAspect"/>.
        /// </summary>
        ChildAspect,

        /// <summary>
        /// The aspect has been required by another aspect using <see cref="AdviserExtensions.RequireAspect"/>.
        /// </summary>
        RequiredAspect,

        /// <summary>
        /// Aspects added because of aspect inheritance.
        /// </summary>
        Inherited,

        /// <summary>
        /// The aspect has been created by a fabric. <see cref="AspectPredecessor.Instance"/> is an <see cref="Fabrics.Fabric"/>.
        /// </summary>
        Fabric,

        /// <summary>
        /// The aspect has been applied interactively by the user, e.g. as a live template.
        /// </summary>
        Interactive
    }
}