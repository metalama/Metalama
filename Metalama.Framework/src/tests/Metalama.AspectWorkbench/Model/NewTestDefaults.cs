// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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