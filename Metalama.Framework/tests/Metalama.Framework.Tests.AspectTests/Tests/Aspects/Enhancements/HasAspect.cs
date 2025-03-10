// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Enhancements.HasAspect
{
    internal class Aspect : OverrideMethodAspect
    {
        public override void BuildAspect( IAspectBuilder<IMethod> builder )
        {
            var targetEnhancements = builder.Target.Enhancements();

            if (!targetEnhancements.HasAspect<Aspect>())
            {
                throw new Exception();
            }

            if (!targetEnhancements.HasAspect<OverrideMethodAspect>())
            {
                throw new Exception();
            }

            var noAspectEnhancements = builder.Target.DeclaringType.Methods.OfName( "NoAspect" ).Single().Enhancements();

            if (noAspectEnhancements.HasAspect<Aspect>())
            {
                throw new Exception();
            }

            if (noAspectEnhancements.HasAspect<OverrideMethodAspect>())
            {
                throw new Exception();
            }
        }

        public override dynamic? OverrideMethod()
        {
            throw new NotImplementedException();
        }
    }

    internal class TargetCode
    {
        // <target>
        [Aspect]
        private int Method( int a )
        {
            return a;
        }

        private void NoAspect() { }
    }
}