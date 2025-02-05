// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.TestApp.Aspects
{
    public class SwallowExceptionsAspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            try
            {
                return meta.Proceed();
            }
            catch ( Exception ex )
            {
                Console.WriteLine( "Metalama caught: " + ex );
                return default;
            }
        }
    }
}
