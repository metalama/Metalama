// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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