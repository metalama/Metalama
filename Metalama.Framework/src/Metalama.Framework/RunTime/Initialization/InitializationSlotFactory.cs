// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading;

namespace Metalama.Framework.RunTime.Initialization;

/// <summary>
/// Factory for allocating <see cref="InitializationSlot"/> instances. Each factory maintains
/// its own counter, allowing tests to allocate slots independently without polluting global state.
/// </summary>
internal class InitializationSlotFactory
{
    private int _nextBit;

    /// <summary>
    /// Allocates a new slot. Maximum 32 slots per factory instance.
    /// </summary>
    /// <returns>A new <see cref="InitializationSlot"/> with a unique bit position.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the maximum of 32 slots has been exceeded.</exception>
    public InitializationSlot Allocate()
    {
        var bit = Interlocked.Increment( ref this._nextBit ) - 1;

        if ( bit >= 32 )
        {
            throw new InvalidOperationException(
                "Cannot allocate an InitializationSlot: the maximum of 32 slots has been exceeded." );
        }

        return new InitializationSlot( 1u << bit );
    }
}
