// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using JetBrains.Annotations;
using System;
using System.Reflection;

namespace PostSharp.Reflection
{
    /// <summary>
    /// In Metalama, use <c>Metalama.Extensions.Validation.ReferenceValidationContext</c>.
    /// </summary>
    [PublicAPI]
    public sealed class MethodUsageCodeReference : ICodeReference
    {
        public MethodBase UsingMethod { get; }

        public MemberInfo UsedDeclaration { get; }

        public Type UsedType { get; }

        public MethodUsageInstructions Instructions { get; }

        object ICodeReference.ReferencingDeclaration { get; }

        object ICodeReference.ReferencedDeclaration { get; }

        CodeReferenceKind ICodeReference.ReferenceKind { get; }
    }
}