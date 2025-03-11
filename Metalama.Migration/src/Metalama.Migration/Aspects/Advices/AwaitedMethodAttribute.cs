// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace PostSharp.Aspects.Advices
{
    /// <summary>
    /// There is no equivalent in Metalama, and there is currently no way to implement this feature differently.
    /// </summary>
    [Obsolete( "", true )]
    public sealed class AwaitedMethodAttribute : AdviceParameterAttribute { }
}