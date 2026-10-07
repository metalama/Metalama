// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace Metalama.Framework.Engine.Extensibility.Synthesis;

/// <summary>
/// Describes the parts of the signature of a method builder, created by <see cref="CallSites.ExtensionTransformationFactory.CreateMethodBuilder"/>,
/// that the code that receives the builder cannot change.
/// </summary>
/// <remarks>
/// <para>
/// An extension that gives a pre-filled builder to user code, for instance the <c>configure</c> delegate of an interceptor, locks the parts of
/// the signature on which its own code depends. A change of a locked part throws an <see cref="InvalidOperationException"/> when it is made,
/// so the user code gets the error at the statement that makes the change.
/// </para>
/// <para>
/// The parameters of a builder cannot be removed or reordered, so the locked leading parameters are always present, in their order.
/// </para>
/// </remarks>
[PublicAPI]
public sealed class SynthesizedMethodRestrictions
{
    /// <summary>
    /// Gets the number of leading parameters whose reference kind cannot change. Their name and their type can change.
    /// </summary>
    public int LockedLeadingParameterCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether the return type cannot change.
    /// </summary>
    public bool IsReturnTypeLocked { get; init; }

    /// <summary>
    /// Gets a value indicating whether type parameters cannot be added, and the constraints of the existing type parameters cannot change.
    /// </summary>
    public bool AreTypeParametersLocked { get; init; }

    /// <summary>
    /// Gets a value indicating whether the name of the method cannot change. The factory can still add a numeric suffix to make it unique.
    /// </summary>
    public bool IsNameLocked { get; init; }
}
