// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Metalama.Backstage.Maintenance;

[PublicAPI]
public enum CleanUpStrategy
{
    // The integer values must not be changed because they are serialized in cleanup.json.

    /// <summary>
    /// The whole directory is never cleaned up.
    /// </summary>
    None = 0,

    /// <summary>
    /// The whole directory is deleted every time the clean up command is executed.
    /// </summary>
    Always = 1,

    /// <summary>
    /// The whole directory is deleted if it has not been for 7 days when the clean up command is executed.
    /// </summary>
    WhenUnused = 2,

    /// <summary>
    /// Individual files are cleaned up one month after they have been created.
    /// </summary>
    FileOneMonthAfterCreation = 3,
    
    /// <summary>
    /// The whole directory is deleted every time the clean up command is executed.
    /// The directory is not moved before deleting.
    /// </summary>
    AlwaysNoMove = 4
}