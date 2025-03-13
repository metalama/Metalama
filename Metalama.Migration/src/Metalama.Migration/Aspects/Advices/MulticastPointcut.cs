// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using PostSharp.Extensibility;

namespace PostSharp.Aspects.Advices
{
    /// <summary>
    /// In Metalama, you should use a programmatic approach. Use a <c>foreach</c> loop in the <see cref="Metalama.Framework.Aspects.IAspect{T}.BuildAspect"/> method, iterate the code model exposed on <c>builder</c>.<see cref="IAspectBuilder{TAspectTarget}.Target"/>, and add advice using methods of <c>builder</c>.<see cref="IAspectBuilder.Advice"/>.
    /// </summary>
    public sealed class MulticastPointcut : Pointcut
    {
        public string MemberName { get; set; }

        public MulticastTargets Targets { get; set; }

        public MulticastAttributes Attributes { get; set; }
    }
}