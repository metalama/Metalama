// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Eligibility;
using PostSharp.Aspects.Configuration;
using PostSharp.Extensibility;
using System;

namespace PostSharp.Aspects
{
    [MulticastAttributeUsage(
        MulticastTargets.Class | MulticastTargets.Struct | MulticastTargets.Interface,
        AllowMultiple = true,
        PersistMetaData = false )]
    [AttributeUsage( AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = true )]
    public abstract class TypeLevelAspect : Aspect, ITypeLevelAspect, ITypeLevelAspectBuildSemantics
    {
        /// <summary>
        /// In Metalama, validation is done in <see cref="IEligible{T}.BuildEligibility"/> and <see cref="IAspect{T}.BuildAspect"/>.
        /// The equivalent of returning <c>false</c> is to call <see cref="IAspectBuilder.SkipAspect"/>.
        /// </summary>
        public virtual bool CompileTimeValidate( Type type )
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="Aspect.CompileTimeValidate" />
        public sealed override bool CompileTimeValidate( object target )
        {
            throw new NotImplementedException();
        }

        protected virtual void SetAspectConfiguration( AspectConfiguration aspectConfiguration, Type targetType ) { }

        protected sealed override void SetAspectConfiguration(
            AspectConfiguration aspectConfiguration,
            object targetElement )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// In Metalama, aspect initialization is done in <see cref="IAspect{T}.BuildAspect"/>.
        /// </summary>
        public virtual void CompileTimeInitialize( Type type, AspectInfo aspectInfo ) { }

        /// <inheritdoc/>
        public virtual void RuntimeInitialize( Type type ) { }
    }
}