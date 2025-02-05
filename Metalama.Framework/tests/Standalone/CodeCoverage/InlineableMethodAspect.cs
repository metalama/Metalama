// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using Metalama.Framework.Aspects;

#pragma warning disable CS0162 // Unreacheable code.

namespace CodeCoverage
{
    public class InlineableMethodAspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            if ( true )
            {
                return meta.Proceed();
            }
            else
            {
                return default;
            }
        }
    }

}