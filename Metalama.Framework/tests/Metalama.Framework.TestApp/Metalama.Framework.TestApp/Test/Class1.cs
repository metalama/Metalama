// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using System.Threading;

namespace Metalama.Framework.TestApp.Test
{
    internal class Class1
    {
        [CancelAspect]
#pragma warning disable IDE0060 // Remove unused parameter
        public void Test( CancellationToken cancellationToken )
#pragma warning restore IDE0060 // Remove unused parameter
        {
        }
    }
}
