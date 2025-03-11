// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace PostSharp.Aspects.Advices
{
    /// <summary>
    /// In Metalama, use a <c>foreach</c> loop in the <see cref="Metalama.Framework.Aspects.IAspect{T}.BuildAspect"/> method, iterate <c>builder</c>.<see cref="IAspectBuilder{TAspectTarget}.Target"/>.<see cref="INamedType"/>.<see cref="INamedType.Methods"/>, and add advice using methods of <c>builder</c>.<see cref="IAspectBuilder.Advice"/>.
    /// </summary>
    [PublicAPI]
    public sealed class MatchPointcut : Pointcut
    {
        public MatchPointcut( string methodName )
        {
            throw new NotImplementedException();
        }

        public bool MatchParameterCount { get; set; } = true;

        public string MethodName { get; }
    }
}