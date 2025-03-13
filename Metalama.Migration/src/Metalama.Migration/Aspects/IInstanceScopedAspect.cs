// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace PostSharp.Aspects
{
    /// <summary>
    /// Not supported in Metalama because aspects no longer have a run-time existence. Instead, they only exist at compile time,
    /// and generate run-time code.
    /// </summary>
    public interface IInstanceScopedAspect : IAspect
    {
        /// <summary>
        /// No equivalent in Metalama.
        /// </summary>
        object CreateInstance( AdviceArgs adviceArgs );

        /// <summary>
        /// Typically, add an initializer using <c>builder</c>.<see cref="IAspectBuilder.Advice"/>.<see cref="IAdviceFactory.AddInitializer(Metalama.Framework.Code.INamedType,string,InitializerKind,object?,object?)"/>.
        /// </summary>
        void RuntimeInitializeInstance();
    }
}