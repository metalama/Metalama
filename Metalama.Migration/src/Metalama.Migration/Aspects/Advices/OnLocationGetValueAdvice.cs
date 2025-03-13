// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

namespace PostSharp.Aspects.Advices
{
    /// <summary>
    /// In Metalama, implement the <see cref="IAspect{T}.BuildAspect"/> method and use
    /// <c>builder</c>.<see cref="IAspectBuilder.Advice"/>.<see cref="M:IAdviceFactory.OverrideAccessors(IFieldOrPropertyOrIndexer, in GetterTemplateSelector, string?, object?, object?)"/>
    /// or <see cref="IAdviceFactory.Override(Metalama.Framework.Code.IFieldOrProperty, string, object?)"/>.
    /// </summary>
    /// <seealso href="@overriding-properties"/>
    [AttributeUsage( AttributeTargets.Method, AllowMultiple = true )]
    public sealed class OnLocationGetValueAdvice : GroupingAdvice { }
}