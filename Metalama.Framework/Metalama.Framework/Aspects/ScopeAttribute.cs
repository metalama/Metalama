// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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