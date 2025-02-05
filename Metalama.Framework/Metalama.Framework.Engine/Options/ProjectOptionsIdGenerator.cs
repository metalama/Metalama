// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Threading;

namespace Metalama.Framework.Engine.Options;

internal static class ProjectOptionsIdGenerator
{
    private static volatile int _nextId;

    public static int GetNextId() => Interlocked.Increment( ref _nextId );
}