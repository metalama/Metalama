// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Aspects.Configuration;
using PostSharp.Extensibility;
using System;

namespace PostSharp.Aspects
{
    /// <summary>
    /// There is no composition aspect in Metalama, but you can build one by deriving the <see cref="TypeAspect"/> type, implementing
    /// the <see cref="TypeAspect.BuildAspect"/> method, and calling <c>builder</c>.<see cref="IAspectBuilder.Advice"/>.<see cref="IAdviceFactory.ImplementInterface(Metalama.Framework.Code.INamedType,Metalama.Framework.Code.INamedType,Metalama.Framework.Aspects.OverrideStrategy,object?)"/>.
    /// There is no concept of protected interface in Metalama.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct,
        AllowMultiple = true,
        Inherited = false )]
    [MulticastAttributeUsage( MulticastTargets.Class | MulticastTargets.Struct, AllowMultiple = true )]
    public abstract class CompositionAspect : TypeLevelAspect, ICompositionAspect
    {
        /// <inheritdoc/>
        public abstract object CreateImplementationObject( AdviceArgs args );

        /// <summary>
        /// The interface type is passed as a parameter to the <see cref="IAdviceFactory.ImplementInterface(Metalama.Framework.Code.INamedType,Metalama.Framework.Code.INamedType,Metalama.Framework.Aspects.OverrideStrategy,object?)"/>
        /// method.
        /// </summary>
        /// <param name="targetType"></param>
        /// <returns></returns>
        protected virtual Type[] GetPublicInterfaces( Type targetType )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Equivalent to the <see cref="OverrideStrategy"/> parameter passed to the <see cref="IAdviceFactory.ImplementInterface(Metalama.Framework.Code.INamedType,Metalama.Framework.Code.INamedType,Metalama.Framework.Aspects.OverrideStrategy,object?)"/>
        /// method.
        /// </summary>
        protected InterfaceOverrideAction OverrideAction { get; set; }

        protected InterfaceOverrideAction AncestorOverrideAction { get; set; }

        /// <summary>
        /// Implementations are always non-serializable by default in Metalama.
        /// </summary>
        protected bool NonSerializedImplementation { get; set; }

        /// <summary>
        /// There is no equivalent to this feature in Metalama.
        /// </summary>
        protected bool GenerateImplementationAccessor { get; set; }

        protected sealed override AspectConfiguration CreateAspectConfiguration()
        {
            throw new NotImplementedException();
        }

        protected override void SetAspectConfiguration( AspectConfiguration aspectConfiguration, Type targetType ) { }
    }
}