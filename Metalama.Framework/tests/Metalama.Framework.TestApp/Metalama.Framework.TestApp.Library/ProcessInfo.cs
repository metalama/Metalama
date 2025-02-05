// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using System;
using System.Diagnostics;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Project;

namespace Metalama.Framework.TestApp.Library
{
    [CompileTime]
    public static class BuildInfo
    {
        public static string GetInfo() => MetalamaExecutionContext.Current.Compilation.Project.TargetFramework;
    }
}
