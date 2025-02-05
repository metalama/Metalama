// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

namespace PostSharp.Reflection
{
    /// <summary>
    /// In Metalama, use <c>Metalama.Extensions.Validation.DeclarationValidationContext</c> or <c>Metalama.Extensions.Validation.ReferenceValidationContext</c>.
    /// </summary>
    public interface ICodeReference
    {
        object ReferencingDeclaration { get; }

        object ReferencedDeclaration { get; }

        CodeReferenceKind ReferenceKind { get; }
    }
}