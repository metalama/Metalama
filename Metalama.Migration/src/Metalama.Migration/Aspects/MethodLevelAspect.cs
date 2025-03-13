// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using PostSharp.Aspects.Configuration;
using PostSharp.Extensibility;
using System;
using System.Reflection;

namespace PostSharp.Aspects
{
    /// <summary>
    /// Equivalent to <see cref="IAspect{T}"/> where <c>T</c> is <see cref="IMethod"/>.
    /// </summary>
    [MulticastAttributeUsage( MulticastTargets.Method | MulticastTargets.InstanceConstructor | MulticastTargets.StaticConstructor )]
    [AttributeUsage(
        AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property |
        AttributeTargets.Event | AttributeTargets.Method | AttributeTargets.Constructor,
        AllowMultiple = true )]
    public abstract class MethodLevelAspect : Aspect, IMethodLevelAspect, IMethodLevelAspectBuildSemantics
    {
        /// <summary>
        /// In Metalama, validation is done in <see cref="IEligible{T}.BuildEligibility"/> and <see cref="IAspect{T}.BuildAspect"/>.
        /// The equivalent of returning <c>false</c> is to call <see cref="IAspectBuilder.SkipAspect"/>.
        /// </summary>
        public virtual bool CompileTimeValidate( MethodBase method )
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="Aspect.CompileTimeValidate" />
        public sealed override bool CompileTimeValidate( object target )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// In Metalama, aspect initialization is done in <see cref="IAspect{T}.BuildAspect"/>.
        /// </summary>
        public virtual void CompileTimeInitialize( MethodBase method, AspectInfo aspectInfo ) { }

        /// <summary>
        /// Not supported in Metalama.
        /// </summary>
        protected virtual void SetAspectConfiguration( AspectConfiguration aspectConfiguration, MethodBase targetMethod ) { }

        /// <inheritdoc/>
        protected sealed override void SetAspectConfiguration(
            AspectConfiguration aspectConfiguration,
            object targetElement )
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public virtual void RuntimeInitialize( MethodBase method ) { }
    }
}