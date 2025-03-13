// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;

namespace PostSharp.Aspects.Advices
{
    /// <summary>
    /// In PostSharp, this delegate allowed the run-time code of the aspect to access a property in the target code. In Metalama,
    /// no run-time helper is required because the template directly generates run-time code.
    /// Use <see cref="Metalama.Framework.Code.IProperty"/>.<see cref="IExpression.Value"/> to generate run-time code for any property.
    /// </summary>
    public delegate TValue PropertyGetter<TValue>();
}