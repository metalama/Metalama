// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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