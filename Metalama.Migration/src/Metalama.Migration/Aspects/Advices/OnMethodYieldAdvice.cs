// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace PostSharp.Aspects.Advices
{
    /// <summary>
    /// There is no equivalent to this advice in Metalama and it is currently not possible to build the equivalent feature.
    /// </summary>
    [Obsolete( "", true )]
    [AttributeUsage( AttributeTargets.Method, AllowMultiple = true )]
    public sealed class OnMethodYieldAdvice : OnMethodBoundaryAdvice { }
}