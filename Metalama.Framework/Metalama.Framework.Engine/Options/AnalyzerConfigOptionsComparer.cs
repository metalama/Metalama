// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static Metalama.Framework.Engine.Options.AnalyzerConfigOptionsBuildProperties;

namespace Metalama.Framework.Engine.Options;

internal sealed class AnalyzerConfigOptionsComparer : IEqualityComparer<AnalyzerConfigOptions>
{
    private readonly ImmutableArray<string>? _allProperties;

    public AnalyzerConfigOptionsComparer( IEnumerable<string>? msbuildProperties )
    {
        this._allProperties = msbuildProperties?.Select( ToAnalyzerConfigName ).ToImmutableArray();
    }

    public bool Equals( AnalyzerConfigOptions? x, AnalyzerConfigOptions? y )
    {
        if ( ReferenceEquals( x, y ) )
        {
            return true;
        }

        if ( x == null || y == null )
        {
            return false;
        }

        var allProperties = this._allProperties ?? x.GetBuildProperties().Union( y.GetBuildProperties() );

        foreach ( var propertyName in allProperties )
        {
            if ( x.TryGetValue( propertyName, out var xProperty ) != y.TryGetValue( propertyName, out var yProperty ) )
            {
                return false;
            }
            else if ( !StringComparer.Ordinal.Equals( xProperty, yProperty ) )
            {
                return false;
            }
        }

        return true;
    }

    public int GetHashCode( AnalyzerConfigOptions obj )
    {
        HashCode hashCode = default;

        var allProperties = this._allProperties ?? obj.GetBuildProperties();

        foreach ( var propertyName in allProperties )
        {
            if ( obj.TryGetValue( propertyName, out var propertyValue ) )
            {
                hashCode.Add( propertyValue, StringComparer.Ordinal );
            }
        }

        return hashCode.ToHashCode();
    }
}