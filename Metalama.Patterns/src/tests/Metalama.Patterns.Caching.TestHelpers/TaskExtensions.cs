// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Caching.TestHelpers
{
    public static class TaskExtensions
    {
        public static async Task<bool> WithTimeout( this Task task, TimeSpan delay )
        {
            return await Task.WhenAny( task, Task.Delay( delay ) ) == task;
        }
    }
}