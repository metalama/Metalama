// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace PostSharp.Aspects.Advices
{
    /// <summary>
    /// In Metalama, implement the <see cref="IAspect{T}.BuildAspect"/> method and use <c>builder</c>.<see cref="IAspectBuilder.Advice"/>.<see cref="IAdviceFactory.OverrideAccessors(Metalama.Framework.Code.IEvent, string?, string?, string?, object?, object?)"/>.
    /// </summary>
    /// <seealso href="@overriding-methods"/>
    public abstract class OnMethodBoundaryAdvice : GroupingAdvice
    {
        /// <summary>
        /// In Metalama, use the different properties of <see cref="MethodTemplateSelector"/>.
        /// </summary>
        public SemanticallyAdvisedMethodKinds SemanticallyAdvisedMethodKinds { get; set; }

        public UnsupportedTargetAction UnsupportedTargetAction { get; set; }
    }
}