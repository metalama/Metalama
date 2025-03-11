// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace PostSharp.Aspects.Advices
{
    /// <summary>
    /// In Metalama, use a <c>foreach</c> loop in the <see cref="Metalama.Framework.Aspects.IAspect{T}.BuildAspect"/> method,use advice using methods of <c>builder</c>.<see cref="IAspectBuilder.Advice"/>
    /// and pass <c>builder</c>.<see cref="IAspectBuilder{TAspectTarget}.Target"/> as the target declaration.
    /// </summary>
    public sealed class SelfPointcut : Pointcut { }
}