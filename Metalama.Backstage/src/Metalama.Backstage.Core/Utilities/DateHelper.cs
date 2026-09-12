// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Backstage.Utilities;

internal static class DateHelper
{
    public static DateTime GetFirstMondayOfMonth( this DateTime date )
    {
        var firstDayOfMonth = new DateTime( date.Year, date.Month, 1 );

        var offset = (DayOfWeek.Monday - firstDayOfMonth.DayOfWeek + 7) % 7;

        return firstDayOfMonth.AddDays( offset );
    }
}