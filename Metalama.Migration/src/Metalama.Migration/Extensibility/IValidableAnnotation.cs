// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Eligibility;

namespace PostSharp.Extensibility
{
    /// <summary>
    /// In Metalama, use <see cref="IAspect{T}"/> and implement <see cref="IEligible{T}.BuildEligibility"/> and/or <see cref="IAspect{T}.BuildAspect"/>.
    /// </summary>
    [RequirePostSharp( null, "ValidateAnnotations" )]
    public interface IValidableAnnotation
    {
        /// <summary>
        /// In Metalama, validation is done in <see cref="IEligible{T}.BuildEligibility"/> and <see cref="IAspect{T}.BuildAspect"/>.
        /// The equivalent of returning <c>false</c> is to call <see cref="IAspectBuilder.SkipAspect"/>.
        /// </summary>
        bool CompileTimeValidate( object target );
    }
}