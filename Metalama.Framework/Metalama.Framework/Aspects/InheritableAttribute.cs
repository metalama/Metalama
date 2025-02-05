// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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