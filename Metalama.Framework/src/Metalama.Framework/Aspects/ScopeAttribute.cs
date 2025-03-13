// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// A base class for all custom attributes that influence the scope (compile-time or run-time) of the code
    /// or its role in an aspect.
    /// </summary>
    [RunTimeOrCompileTime]
    public abstract class ScopeAttribute : Attribute
    {
        private protected ScopeAttribute() { }
    }
}