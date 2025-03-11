// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace PostSharp.Reflection
{
    /// <summary>
    /// In Metalama, use <c>Metalama.Extensions.Validation.ReferenceValidationContext</c>.
    /// </summary>
    [PublicAPI]
    public sealed class MemberTypeCodeReference : ICodeReference
    {
        public Type Type { get; }

        public object Member { get; }

        object ICodeReference.ReferencingDeclaration { get; }

        object ICodeReference.ReferencedDeclaration { get; }

        CodeReferenceKind ICodeReference.ReferenceKind { get; }
    }
}