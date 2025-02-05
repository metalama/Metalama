// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using Metalama.Framework.Code;

namespace PostSharp.Reflection
{
    /// <summary>
    /// In Metalama, use <see cref="ReferenceKinds"/>.
    /// </summary>
    public enum CodeReferenceKind
    {
        None = 0,

        TypeInheritance = 1,

        MemberType = 2,

        MethodUsage = 16
    }
}