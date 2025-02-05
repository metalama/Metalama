// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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