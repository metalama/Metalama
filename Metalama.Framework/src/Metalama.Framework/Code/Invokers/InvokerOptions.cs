// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Code.Invokers;

/// <summary>
/// Options that influence the behavior of invokers, i.e. <see cref="IMethodInvoker"/>, <see cref="IFieldOrPropertyInvoker"/>,
/// <see cref="IEventInvoker"/> or <see cref="IIndexerInvoker"/>.
/// </summary>
[CompileTime]
[Flags]
public enum InvokerOptions
{
    /// <summary>
    /// When the invoked member is the target member of the current template (i.e. <c>meta.Target.Declaration</c>), and when no target instance or type is explicitly specified,
    /// for the invoker, equivalent to <see cref="Base"/>. Otherwise, equivalent to <see cref="Final"/>.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Generates calls to the <i>current</i> implementation, i.e. after all overrides or introductions by the current aspect layer,
    /// but before any next aspect layer or any derived type. 
    /// </summary>
    Current = 1,

    /// <summary>
    /// Generates calls to the <i>base</i> implementation, i.e. before any override or introduction by the current aspect layer. 
    /// </summary>
    Base = 2,

    /// <summary>
    /// Causes the <i>final</i> implementation to be called, i.e. the implementation after all overrides by aspects
    /// and, if the member is <c>virtual</c>, by derived classes through the <c>override</c> keyword. 
    /// </summary>
    Final = 3,

    /// <summary>
    /// Mask for bits that encode order values.
    /// </summary>
    OrderMask = 0x0f,

    /// <summary>
    /// Specifies that the null-conditional access operator (<c>?.</c> aka Elvis) has to be used instead of the dot operator. 
    /// </summary>
    NullConditional = 1024
}