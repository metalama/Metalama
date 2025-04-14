// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Metalama.Framework.Engine.Utilities.AssemblyLoaders;

internal static class AssemblyLoaderFactory
{
    public static AssemblyLoader CreateAssemblyLoader(
        Func<string, Assembly?> resolveAssembly,
        Func<Assembly?, bool>? globalResolveHandlerFilter = null,
        string? debugName = null )
    {
        if ( RuntimeInformation.FrameworkDescription.StartsWith( ".NET Framework", StringComparison.Ordinal ) )
        {
            return new NetFrameworkAssemblyLoader( resolveAssembly, debugName );
        }
        else
        {
            return new NetCoreAssemblyLoader( resolveAssembly, globalResolveHandlerFilter, debugName );
        }
    }
}