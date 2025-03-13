// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Engine.Utilities;

public static class RandomIdGenerator
{
    private static readonly Random _random = new();

    public static string GenerateId()
    {
        lock ( _random )
        {
            return $"{_random.Next():x}{_random.Next():x}";
        }
    }
}