// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

namespace PostSharp.Constraints
{
    /// <summary>
    /// In Metalama, use an aspect or a fabric, and register a reference validator using the  <c>Metalama.Extensions.Validation.ReferenceValidationQueryExtensions.ValidateInboundReferences</c> method.
    /// </summary>
    /// <seealso href="@aspect-validating"/>
    public interface IScalarConstraint : IConstraint
    {
        void ValidateCode( object target );
    }
}