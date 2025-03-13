// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;

namespace PostSharp.Aspects.Advices
{
    /// <summary>
    /// Bindings do not exist in Metalama. Instead, use invokers (e.g. <see cref="IMethod"/>.<see cref="IMethod.Invoke(object[])"/>) to generate run-time
    /// code that invokes the desired method or accesses the property or event.
    /// </summary>
    public sealed class BindingAttribute : AdviceParameterAttribute { }
}