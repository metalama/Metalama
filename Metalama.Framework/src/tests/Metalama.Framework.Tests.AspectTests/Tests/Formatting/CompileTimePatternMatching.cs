// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Highlighting.IfStatements.CompileTimePatternMatching
{
    [CompileTime]
    enum SwitchEnum
    {
        one = 1,
        two = 2,
    }

    class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            switch (meta.Target.Parameters)
            {
                case null:
                    Console.WriteLine("1");
                    break;
                case IEnumerable<IParameter> enumerable when enumerable.Any():
                    meta.InsertComment(enumerable.Count().ToString());
                    break;
                case IEnumerable<IParameter> enumerable when !enumerable.Any():
                    meta.InsertComment("none");
                    break;
                default:
                    break;
            }
            
            return meta.Proceed();
        }
    }
    
}
