// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System;

namespace Metalama.Backstage.Maintenance;

[Flags]
internal enum KillableModuleKind
{
    [UsedImplicitly]
    None,
    StandaloneProcess = 1,
    DotNet = 2,
    Both = 3
}