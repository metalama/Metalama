// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Custom attribute that, when applied to an aspect class, means that instances of this aspect
    /// are inherited from the base class or interface to derived classes, from base methods to method overrides,
    /// from interface methods to method implementations, and so on. 
    /// </summary>
    [AttributeUsage( AttributeTargets.Class )]
    [CompileTime]
    [PublicAPI]
    public sealed class InheritableAttribute : Attribute;
}