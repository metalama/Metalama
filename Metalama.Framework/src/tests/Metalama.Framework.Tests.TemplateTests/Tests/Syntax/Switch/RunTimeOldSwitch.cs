// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.Switch.DefaultInOldSwitchRunTime
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var i = 1;

            switch (i)
            {
                case 0:
                    Console.WriteLine( "0" );

                    break;

                default:
                    Console.WriteLine( "Default" );

                    break;
            }

            return meta.Proceed();
        }
    }

    internal class TargetCode
    {
        private int Method( int a )
        {
            return a;
        }
    }
}