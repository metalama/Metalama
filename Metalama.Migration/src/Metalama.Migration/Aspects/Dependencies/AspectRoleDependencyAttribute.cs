// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace PostSharp.Aspects.Dependencies
{
    /// <summary>
    /// Aspect roles are not supported in Metalama.
    /// </summary>
    [PublicAPI]
    public sealed class AspectRoleDependencyAttribute : AspectDependencyAttribute
    {
        public AspectRoleDependencyAttribute(
            AspectDependencyAction action,
            AspectDependencyPosition position,
            string role )
            : base( action, position )
        {
            throw new NotImplementedException();
        }

        public AspectRoleDependencyAttribute( AspectDependencyAction action, string role )
            : base( action )
        {
            throw new NotImplementedException();
        }

        public string Role { get; }
    }
}