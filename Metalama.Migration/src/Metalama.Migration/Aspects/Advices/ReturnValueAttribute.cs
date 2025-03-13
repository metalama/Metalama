// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace PostSharp.Aspects.Advices
{
    /// <summary>
    /// In Metalama, call <c>meta.Proceed</c> in the template and store the value in a local variable.
    /// </summary>
    public sealed class ReturnValueAttribute : AdviceParameterAttribute { }
}