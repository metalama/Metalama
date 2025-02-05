// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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