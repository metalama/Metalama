// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using System.Linq;
using System.Threading;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.TestApp
{
    internal class CancelAspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            var parameter = meta.Target.Parameters.LastOrDefault( p => p.Type.IsConvertibleTo( typeof( CancellationToken ) ) );

            if ( parameter != null )
            {
                parameter.Value!.ThrowIfCancellationRequested();
            }

            return meta.Proceed();
        }
    }
}
