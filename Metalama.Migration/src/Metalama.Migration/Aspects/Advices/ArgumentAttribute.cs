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
    /// In Metalama, run-time advice parameters are matched by name, not by index. If you need to get a parameter by index,
    /// use <see cref="meta"/>.<see cref="meta.Target"/>.<see cref="IMetaTarget.Parameters"/><c>[index]</c>.<see cref="IExpression.Value"/>.
    /// from the template.
    /// </summary>
    /// <seealso href="@template-parameters"/>
    [PublicAPI]
    public sealed class ArgumentAttribute : AdviceParameterAttribute
    {
        public int Index { get; }

        public ArgumentAttribute( int index )
        {
            throw new NotImplementedException();
        }
    }
}