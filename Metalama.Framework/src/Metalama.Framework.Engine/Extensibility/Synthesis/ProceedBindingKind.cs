// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Framework.Engine.Extensibility.Synthesis;

/// <summary>
/// Kinds of <see cref="ProceedBinding"/>.
/// </summary>
[PublicAPI]
public enum ProceedBindingKind
{
    /// <summary>
    /// The default value, which is not valid.
    /// </summary>
    None,

    /// <summary>
    /// <c>meta.Proceed()</c> invokes a static method, written <c>T.M( arguments )</c>.
    /// </summary>
    InvokeStatic,

    /// <summary>
    /// <c>meta.Proceed()</c> invokes an instance method on a parameter of the declared method, written <c>p.M( arguments )</c>.
    /// </summary>
    InvokeOnParameter
}
