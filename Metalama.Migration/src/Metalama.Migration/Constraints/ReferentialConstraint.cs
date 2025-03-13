// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Reflection;

namespace PostSharp.Constraints
{
    /// <summary>
    /// In Metalama, use an aspect or a fabric, and register a reference validator using the <c>Metalama.Extensions.Validation.ReferenceValidationQueryExtensions.ValidateInboundReferences</c> method.
    /// </summary>
    /// <seealso href="@aspect-validating"/>
    /// <seealso href="@validating-usage"/>
    public abstract class ReferentialConstraint : Constraint, IReferentialConstraint
    {
        public virtual void ValidateCode( object target, Assembly assembly ) { }
    }
}