// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Application;

namespace Metalama.Tool
{
    internal sealed class ApplicationInfo : ApplicationInfoBase
    {
        public ApplicationInfo() : base( typeof(ApplicationInfo).Assembly ) { }

        public override string Name => typeof(ApplicationInfo).Assembly.GetName().Name!;
    }
}