// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
            var parameter = meta.Target.Parameters.LastOrDefault(p => p.Type.IsConvertibleTo(typeof(CancellationToken)));

            if (parameter != null)
            {
                parameter.Value!.ThrowIfCancellationRequested();
            }

            return meta.Proceed();
        }
    }
}
