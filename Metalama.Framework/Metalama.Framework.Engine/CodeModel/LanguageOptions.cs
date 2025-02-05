// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Comparers;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.CodeModel;

/// <summary>
/// Contains the subset of <see cref="CSharpParseOptions"/> that must be identical for all syntax trees in the compilation.
/// </summary>
internal sealed class LanguageOptions : IEquatable<LanguageOptions>
{
    private static readonly StructuralDictionaryComparer<string, string> _featureComparer = new( EqualityComparer<string>.Default );
    private readonly ImmutableDictionary<string, string> _features;

    public LanguageVersion Version { get; }

    private LanguageOptions( LanguageVersion version, ImmutableDictionary<string, string> features )
    {
        this.Version = version;
        this._features = features;
    }

    public static LanguageOptions Default { get; } = new( SupportedCSharpVersions.Default, ImmutableDictionary<string, string>.Empty );

    internal LanguageOptions( CSharpParseOptions options ) : this( options.LanguageVersion, options.Features.ToImmutableDictionary() ) { }

    internal CSharpParseOptions ToParseOptions()
        => SupportedCSharpVersions.DefaultParseOptions.WithLanguageVersion( this.Version ).WithFeatures( this._features );

    public bool Equals( LanguageOptions? other )
    {
        if ( ReferenceEquals( null, other ) )
        {
            return false;
        }

        if ( ReferenceEquals( this, other ) )
        {
            return true;
        }

        return this.Version == other.Version && _featureComparer.Equals( this._features, other._features );
    }

    public override bool Equals( object? obj )
    {
        if ( ReferenceEquals( null, obj ) )
        {
            return false;
        }

        if ( ReferenceEquals( this, obj ) )
        {
            return true;
        }

        if ( obj.GetType() != this.GetType() )
        {
            return false;
        }

        return this.Equals( (LanguageOptions) obj );
    }

    public override int GetHashCode() => HashCode.Combine( (int) this.Version, _featureComparer.GetHashCode( this._features ) );

    public static bool operator ==( LanguageOptions? left, LanguageOptions? right ) => Equals( left, right );

    public static bool operator !=( LanguageOptions? left, LanguageOptions? right ) => !Equals( left, right );
}