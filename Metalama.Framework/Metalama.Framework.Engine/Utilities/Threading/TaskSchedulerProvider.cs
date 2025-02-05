// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Utilities.Threading;

internal static class TaskSchedulerProvider
{
    // Roslyn seems to use the default task scheduler, so do we.
    public static TaskScheduler TaskScheduler => TaskScheduler.Default;
}