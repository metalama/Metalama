// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace PostSharp.Aspects.Advices
{
    /// <summary>
    /// In Metalama, there is no need for <see cref="FlowBehavior"/> because the template is in full control
    /// of the generated code.
    /// </summary>
    public sealed class FlowBehaviorAttribute : AdviceParameterAttribute { }
}