// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

namespace PostSharp.Aspects.Advices
{
    /// <summary>
    /// In Metalama, there is no declarative way to bind advice parameters. Instead, Metalama uses a programmatic approach, where the advice can get the desired
    /// value using the code model. In Metalama, template parameters can be marked as compile-time using <see cref="CompileTimeAttribute"/>, otherwise they are run-time.
    /// Run-time parameters must match the target parameter by name. Compile-time parameters must be supplied by the advice factory method (see <see cref="IAdviceFactory"/>).
    /// </summary>
    /// <seealso href="@template-parameters"/>
    [AttributeUsage( AttributeTargets.Parameter )]
    public abstract class AdviceParameterAttribute : Attribute { }
}