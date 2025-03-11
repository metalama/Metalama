// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using PostSharp.Aspects.Configuration;
using PostSharp.Extensibility;
using System;

namespace PostSharp.Aspects
{
    /// <summary>
    /// In Metalama, use <see cref="TypeAspect"/>. Note that aspects in Metalama have no run-time existence, so they cannot be instance-scoped.
    /// While porting a PostSharp <see cref="InstanceLevelAspect"/> into Metalama, you would typically introduce instance fields or properties into
    /// the target type, and use an initializer to initialize them.
    /// </summary>
    [MulticastAttributeUsage( MulticastTargets.Class, TargetTypeAttributes = MulticastAttributes.Instance )]
    [AttributeUsage( AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true )]
    [PublicAPI]
    public abstract class InstanceLevelAspect : TypeLevelAspect, ICloneAwareAspect
    {
        /// <summary>
        /// No equivalent in Metalama.
        /// </summary>
        public virtual object CreateInstance( AdviceArgs adviceArgs )
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public virtual void RuntimeInitializeInstance() { }

        /// <summary>
        /// Not implemented in Metalama.
        /// </summary>
        [Obsolete( "", true )]
        public virtual void OnCloned( ICloneAwareAspect source ) { }

        /// <inheritdoc/>
        protected override AspectConfiguration CreateAspectConfiguration()
        {
            return new AspectConfiguration();
        }

        /// <summary>
        /// Use <see cref="meta"/>.<see cref="meta.This"/>.
        /// </summary>
        public object Instance { get; }
    }
}