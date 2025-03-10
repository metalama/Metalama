// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;

namespace Metalama.Backstage.Worker
{
    internal class BackstageWorkerApplicationInfo : ApplicationInfoBase
    {
        public BackstageWorkerApplicationInfo()
            : base( typeof(BackstageWorkerApplicationInfo).Assembly ) { }

        public override string Name => "Metalama Backstage Worker";

        public override ProcessKind ProcessKind => ProcessKind.BackstageWorker;

        public override bool IsLongRunningProcess => false;

        public override bool IsUnattendedProcess( ILoggerFactory loggerFactory ) => true;
    }
}