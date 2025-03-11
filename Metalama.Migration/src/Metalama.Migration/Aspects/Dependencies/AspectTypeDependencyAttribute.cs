// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using System;

namespace PostSharp.Aspects.Dependencies
{
    /// <summary>
    /// In Metalama, use <see cref="AspectOrderAttribute"/> to specify order dependencies (typically one attribute per aspect library).
    /// The other kinds of dependencies are not supported in Metalama.
    /// </summary>
    [PublicAPI]
    public sealed class AspectTypeDependencyAttribute : AspectDependencyAttribute
    {
        public AspectTypeDependencyAttribute(
            AspectDependencyAction action,
            AspectDependencyPosition position,
            Type aspectType )
            : base( action, position )
        {
            throw new NotImplementedException();
        }

        public AspectTypeDependencyAttribute( AspectDependencyAction action, Type aspectType )
            : base( action )
        {
            throw new NotImplementedException();
        }

        public Type AspectType { get; }
    }
}