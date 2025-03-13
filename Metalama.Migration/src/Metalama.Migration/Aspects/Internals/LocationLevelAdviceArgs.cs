// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using PostSharp.Reflection;

namespace PostSharp.Aspects.Internals
{
    /// <summary>
    /// In PostSharp, this object exposed the run-time execution context to the advice. However, in Metalama, advice do not execute at run time.
    /// Instead, advice are templates that generate run-time code. This run-time code does not need helper objects to represent the execution context.
    /// </summary>
    public abstract class LocationLevelAdviceArgs : AdviceArgs
    {
        internal LocationLevelAdviceArgs() { }

        /// <summary>
        /// In Metalama, use the <c>value</c> template parameter.
        /// </summary>
        public abstract object Value { get; set; }

        /// <summary>
        /// In Metalama, use <see cref="meta"/>.<see cref="meta.Target"/>.<see cref="IMetaTarget.FieldOrProperty"/>. If you need
        /// a run-time object, use <see cref="IFieldOrProperty"/>.<see cref="IFieldOrProperty.ToFieldOrPropertyInfo"/>.
        /// </summary>
        public LocationInfo Location { get; set; }

        /// <summary>
        /// In Metalama, use <see cref="meta"/>.<see cref="meta.Target"/>.<see cref="IMetaTarget.FieldOrProperty"/>.<see cref="INamedDeclaration.Name"/>.
        /// </summary>
        public string LocationName { get; set; }

        /// <summary>
        /// Not implemented in Metalama.
        /// </summary>
        public string LocationFullName { get; set; }
    }
}