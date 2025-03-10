// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug30599
{
    internal class Aspect : MethodAspect
    {
        public override void BuildAspect( IAspectBuilder<IMethod> builder )
        {
            builder.Override( nameof(Template) );
        }

        [Template]
        public dynamic? Template()
        {
            var disposedField = meta.Target.Method.DeclaringType.Fields.OfName( "_disposed" ).FirstOrDefault();

            if ((bool)disposedField!.Value)
            {
                throw new InvalidOperationException( "The object has already been disposed" );
            }

            return meta.Proceed();
        }
    }

    // <target>
    internal class TargetCode
    {
        public bool _disposed;

        [Aspect]
        private async Task<int> Method( int a )
        {
            await Task.Yield();

            return a;
        }
    }
}