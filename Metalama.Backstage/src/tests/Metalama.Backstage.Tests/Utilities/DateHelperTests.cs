// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Utilities;
using System;
using System.Globalization;
using Xunit;

namespace Metalama.Backstage.Tests.Utilities;

public sealed class DateHelperTests
{
    [Theory]
    [InlineData( "2025-05-05", "2025-05-05" )]
    [InlineData( "2025-05-06", "2025-05-05" )]
    [InlineData( "2025-05-07", "2025-05-05" )]
    [InlineData( "2025-05-08", "2025-05-05" )]
    [InlineData( "2025-05-09", "2025-05-05" )]
    [InlineData( "2025-05-10", "2025-05-05" )]
    [InlineData( "2025-05-11", "2025-05-05" )]
    public void Test( string input, string expectedOutput )
    {
        Assert.Equal( Parse( expectedOutput ), Parse( input ).GetFirstMondayOfMonth() );
    }

    private static DateTime Parse( string dateString )
    {
        if ( DateTime.TryParseExact( dateString, "yyyy-MM-dd", null, DateTimeStyles.None, out var parsedDate ) )
        {
            return parsedDate;
        }
        else
        {
            throw new FormatException( "Invalid date format. Expected format is YYYY-MM-DD." );
        }
    }
}