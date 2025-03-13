// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace PostSharp.Aspects
{
    /// <summary>
    /// There is no specific aspect class to add a custom attribute in Metalama, but only the advice factory method
    /// <see cref="IntroduceAttribute"/>. Create an aspect class that calls this advice factory method
    /// from <see cref="IAspect{T}.BuildAspect"/>.
    /// </summary>
    public interface ICustomAttributeIntroductionAspect : IAspect { }
}