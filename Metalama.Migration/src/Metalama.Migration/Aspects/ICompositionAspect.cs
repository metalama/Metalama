// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace PostSharp.Aspects
{
    /// <summary>
    /// There is no composition aspect in Metalama, but you can build one by deriving the <see cref="TypeAspect"/> type, implementing
    /// the <see cref="TypeAspect.BuildAspect"/> method, and calling <c>builder</c>.<see cref="IAspectBuilder.Advice"/>.<see cref="IAdviceFactory.ImplementInterface(Metalama.Framework.Code.INamedType,Metalama.Framework.Code.INamedType,Metalama.Framework.Aspects.OverrideStrategy,object?)"/>.
    /// There is no concept of protected interface in Metalama.
    /// </summary>
    public interface ICompositionAspect : ITypeLevelAspect
    {
        /// <summary>
        /// In Metalama, you would typically have this code in an initializer added to the type using
        /// <see cref="IAdviceFactory.AddInitializer(Metalama.Framework.Code.INamedType,string,InitializerKind,object?,object?)"/>. 
        /// </summary>
        object CreateImplementationObject( AdviceArgs args );
    }
}