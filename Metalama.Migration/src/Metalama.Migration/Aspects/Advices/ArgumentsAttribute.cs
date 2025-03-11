// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Collections;

namespace PostSharp.Aspects.Advices
{
    /// <summary>
    /// In Metalama, do not use a parameter but use <see cref="meta"/>.<see cref="meta.Target"/>.<see cref="IMetaTarget.Parameters"/>.<see cref="IParameterList.ToValueArray"/>
    /// from the template implementation.
    /// </summary>
    /// <seealso href="@template-parameters"/>
    public sealed class ArgumentsAttribute : AdviceParameterAttribute { }
}