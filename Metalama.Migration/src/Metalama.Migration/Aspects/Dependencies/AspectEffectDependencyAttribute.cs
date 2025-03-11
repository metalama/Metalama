// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace PostSharp.Aspects.Dependencies
{
    /// <summary>
    /// Aspect effects are not supported in Metalama.
    /// </summary>
    [PublicAPI]
    public sealed class AspectEffectDependencyAttribute : AspectDependencyAttribute
    {
        public AspectEffectDependencyAttribute(
            AspectDependencyAction action,
            AspectDependencyPosition position,
            string effect )
            : base( action, position )
        {
            throw new NotImplementedException();
        }

        public AspectEffectDependencyAttribute( AspectDependencyAction action, string effect )
            : base( action )
        {
            throw new NotImplementedException();
        }

        public string Effect { get; }
    }
}