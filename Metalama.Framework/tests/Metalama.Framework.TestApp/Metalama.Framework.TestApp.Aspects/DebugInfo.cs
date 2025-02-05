// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.TestApp.Library;

namespace Metalama.Framework.TestApp.Aspects
{
    [CompileTime]
    public static class DebugInfo
    {
        public static string GetInfo() => BuildInfo.GetInfo();
    }
}
