// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.Options;

internal static class AnalyzerConfigOptionsBuildProperties
{
    private const string _prefix = "build_property.";

    public static string ToAnalyzerConfigName( string msBuildPropertyName ) => _prefix + msBuildPropertyName;

    public static string ToMsBuildPropertyName( string analyzerConfigName )
    {
        Invariant.Assert( analyzerConfigName.StartsWith( _prefix, StringComparison.Ordinal ) );

        return analyzerConfigName.Substring( _prefix.Length );
    }

    public static IEnumerable<string> GetBuildProperties( this AnalyzerConfigOptions options )
    {
        return options.Keys.Where( name => name.StartsWith( _prefix, StringComparison.Ordinal ) );
    }
}