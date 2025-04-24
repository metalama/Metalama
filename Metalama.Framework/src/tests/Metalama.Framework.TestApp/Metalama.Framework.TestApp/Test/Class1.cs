// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Threading;

namespace Metalama.Framework.TestApp.Test
{
    internal class Class1
    {
        [CancelAspect]
#pragma warning disable IDE0060 // Remove unused parameter
        public void Test(CancellationToken cancellationToken)
#pragma warning restore IDE0060 // Remove unused parameter
        {
        }
    }
}
