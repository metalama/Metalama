// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Options;
using Metalama.Framework.Serialization;

namespace Metalama.Patterns.Wpf.Implementation.NamingConvention;

[CompileTime]
internal sealed class NamingConventionRegistration<T> : IIncrementalKeyedCollectionItem<string>
    where T : class, INamingConvention, ICompileTimeSerializable
{
    public NamingConventionRegistration( string key, T? namingConvention, int? priority = null )
    {
        if ( string.IsNullOrWhiteSpace( key ) )
        {
            throw new ArgumentException( "Must not be null, empty or only white space.", nameof(key) );
        }

        this.Key = key;
        this.NamingConvention = namingConvention;
        this.Priority = priority;
    }

    public string Key { get; }

    object IIncrementalObject.ApplyChanges( object changes, in ApplyChangesContext context )
    {
        var other = (NamingConventionRegistration<T>) changes;

        return new NamingConventionRegistration<T>( this.Key, other.NamingConvention ?? this.NamingConvention, other.Priority ?? this.Priority );
    }

    public T? NamingConvention { get; }

    public int? Priority { get; }
}