// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace PostSharp.Aspects
{
    /// <summary>
    /// There is no equivalent of this aspect in Metalama because Metalama rewrites the code in such a way that field or property initializations
    /// go through the overridden setter. This is done by moving the initializers to the constructor, where it is safe to call the aspect.
    /// </summary>
    public interface IOnInstanceLocationInitializedAspect : ILocationLevelAspect
    {
        /// <summary>
        /// There is no equivalent in Metalama because field and property initializers go through the overridden setter.
        /// </summary>
        void OnInstanceLocationInitialized( LocationInitializationArgs args );
    }
}