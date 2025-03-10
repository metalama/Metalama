// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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