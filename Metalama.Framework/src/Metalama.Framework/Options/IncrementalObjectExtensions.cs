// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Options;

/// <summary>
/// Extensions of the <see cref="IIncrementalObject"/> interface.
/// </summary>
[CompileTime]
public static class IncrementalObjectExtensions
{
    /// <summary>
    /// Invokes <see cref="IIncrementalObject.ApplyChanges"/> in a type- and nullable-safe way.
    /// </summary>
    public static T? ApplyChangesSafe<T>( this T? baseOptions, T? overrideOptions, in ApplyChangesContext context )
        where T : class, IIncrementalObject
    {
        if ( baseOptions == null )
        {
            return overrideOptions;
        }
        else if ( overrideOptions == null )
        {
            return baseOptions;
        }
        else
        {
            return (T) baseOptions.ApplyChanges( overrideOptions, context );
        }
    }

    /// <summary>
    /// Invokes <see cref="IIncrementalObject.ApplyChanges"/> in a type--safe way.
    /// </summary>
    public static T ApplyChanges<T>( this T baseOptions, T overrideOptions, in ApplyChangesContext context )
        where T : class, IIncrementalObject
        => (T) baseOptions.ApplyChanges( overrideOptions, context );
}