// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.AspectWorkbench.Model
{
    internal static class NewTestDefaults
    {
        public const string TemplateSource =
            @"using System;
using System.Collections.Generic;
using Metalama.Framework;
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Code;

namespace $ns
{
    class Aspect : Attribute
    {
    }

    class TargetCode
    {
        [Aspect]
        int Method(int a)
        {
            return a;
        }
    }
}";
    }
}