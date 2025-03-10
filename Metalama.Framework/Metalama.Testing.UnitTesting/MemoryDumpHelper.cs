// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using JetBrains.Profiler.SelfApi;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Utilities;
using Metalama.Framework.Engine;
using System.IO;
using System.Threading;

namespace Metalama.Testing.UnitTesting;

[PublicAPI]
public static class MemoryDumpHelper
{
    private static int _counter;

    public static string? CaptureMiniDumpOnce()
    {
        if ( Interlocked.Increment( ref _counter ) == 1 )
        {
            var dumper = BackstageServiceFactory.ServiceProvider.GetBackstageService<IMiniDumper>();

            return dumper?.Write();
        }
        else
        {
            // Do not capture more than one dump during tests because it can make things very slow
            // and give the impression that tests are stuck.
            return null;
        }
    }

#if NET6_0_OR_GREATER || NETFRAMEWORK
    public static void CaptureDotMemoryDumpAndThrow( string reason )
    {
        if ( Interlocked.Increment( ref _counter ) == 1 )
        {
            DotMemory.Init();
            var dotMemoryConfig = new DotMemory.Config();
            var path = Path.Combine( MetalamaPathUtilities.GetTempPath(), "Metalama", "MemoryDumps" );
            dotMemoryConfig.SaveToDir( path );

            DotMemory.GetSnapshotOnce( dotMemoryConfig );

            throw new AssertionFailedException( $"A memory leak was detected. Inspect the dump file in '{path}'. {reason}" );
        }
        else
        {
            throw new AssertionFailedException( $"A memory leak was detected. A dump file was already created in this process'. {reason}" );
        }
    }
#endif
}