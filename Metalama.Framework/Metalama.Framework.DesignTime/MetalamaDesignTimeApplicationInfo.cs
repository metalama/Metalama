// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Application;
using Metalama.Compiler;

namespace Metalama.Framework.DesignTime
{
    internal sealed class MetalamaDesignTimeApplicationInfo : ApplicationInfoBase
    {
        public override string Name => "Metalama.DesignTime";

        public override bool IsLongRunningProcess => !MetalamaCompilerInfo.IsActive;

        public MetalamaDesignTimeApplicationInfo() : base( typeof(MetalamaDesignTimeApplicationInfo).Assembly ) { }
    }
}