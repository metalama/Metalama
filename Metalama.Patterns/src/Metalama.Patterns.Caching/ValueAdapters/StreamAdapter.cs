// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Caching.ValueAdapters;

internal sealed class StreamAdapter : ValueAdapter<Stream>
{
    public override bool IsAsyncSupported => true;

    public override object? GetStoredValue( Stream? value )
    {
        if ( value == null )
        {
            return null;
        }

        var buffer = new byte[8192];

        using ( var memoryStream = new MemoryStream() )
        {
            int bytes;

            while ( (bytes = value.Read( buffer, 0, buffer.Length )) > 0 )
            {
                memoryStream.Write( buffer, 0, bytes );
            }

            return memoryStream.ToArray();
        }
    }

    public override async Task<object?> GetStoredValueAsync( Stream? value, CancellationToken cancellationToken )
    {
        if ( value == null )
        {
            return null;
        }

        var buffer = new byte[8192];

        using ( var memoryStream = new MemoryStream() )
        {
            int bytes;

            while ( (bytes = await value.ReadAsync( buffer, 0, buffer.Length, cancellationToken )) > 0 )
            {
                memoryStream.Write( buffer, 0, bytes );
            }

            return memoryStream.ToArray();
        }
    }

    public override Stream? GetExposedValue( object? storedValue )
    {
        if ( storedValue == null )
        {
            return null;
        }

        var buffer = (byte[]) storedValue;

        return new MemoryStream( buffer, false );
    }
}