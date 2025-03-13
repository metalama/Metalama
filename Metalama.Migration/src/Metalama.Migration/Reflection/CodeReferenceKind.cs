// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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