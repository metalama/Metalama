// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Backstage.Maintenance;

internal enum CleanUpAction
{
    CleanUpSubdirectories,
    MoveAndDeleteDirectory,
    DeleteDirectory,
    DeleteOneMonthOldFilesFirst,
    Delete4HoursOldFilesFirst
}