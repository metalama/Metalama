// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Threading;

namespace Metalama.Framework.Engine.Options;

internal static class ProjectOptionsIdGenerator
{
    private static volatile int _nextId;

    public static int GetNextId() => Interlocked.Increment( ref _nextId );
}