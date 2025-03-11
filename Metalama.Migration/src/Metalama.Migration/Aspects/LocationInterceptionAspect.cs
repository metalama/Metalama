// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using PostSharp.Aspects.Configuration;
using PostSharp.Extensibility;
using System;

namespace PostSharp.Aspects
{
    /// <summary>
    /// In Metalama, use <see cref="OverrideFieldOrPropertyAspect"/>.
    /// </summary>
    [MulticastAttributeUsage(
        MulticastTargets.Field | MulticastTargets.Property,
        TargetMemberAttributes = MulticastAttributes.NonLiteral | MulticastAttributes.NonAbstract,
        AllowMultiple = true )]
    [AttributeUsage(
        AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Interface
        | AttributeTargets.Property | AttributeTargets.Struct,
        AllowMultiple = true )]
    public abstract class LocationInterceptionAspect : LocationLevelAspect, ILocationInterceptionAspect, IOnInstanceLocationInitializedAspect
    {
        /// <inheritdoc/>
        public virtual void OnGetValue( LocationInterceptionArgs args )
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public virtual void OnSetValue( LocationInterceptionArgs args )
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public virtual void OnInstanceLocationInitialized( LocationInitializationArgs args )
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected sealed override AspectConfiguration CreateAspectConfiguration()
        {
            throw new NotImplementedException();
        }
    }
}