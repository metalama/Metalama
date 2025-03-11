// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Concurrent;

namespace Metalama.Framework.Engine.Analyzers
{
    internal static class ProjectClassifier
    {
        private static readonly ConcurrentDictionary<string, ProjectKind> _cache = new();

        public static ProjectKind GetProjectKind( string name ) => _cache.GetOrAdd( name, GetProjectKindCore );

        private static ProjectKind GetProjectKindCore( string name )
        {
            if ( name.StartsWith( "Metalama.Framework.Engine.", StringComparison.Ordinal ) ||
                 name.StartsWith( "Metalama.Framework.DesignTime.", StringComparison.Ordinal ) )
            {
                return ProjectKind.MetalamaInternal;
            }
            else if ( name == "Metalama.Framework.Workspaces" ||
                      name.StartsWith( "Metalama.Testing.UnitTesting", StringComparison.Ordinal ) ||
                      name.StartsWith( "Metalama.Testing.AspectTesting", StringComparison.Ordinal ) )
            {
                return ProjectKind.MetalamaPublicApi;
            }
            else
            {
                return ProjectKind.UserProject;
            }
        }
    }
}