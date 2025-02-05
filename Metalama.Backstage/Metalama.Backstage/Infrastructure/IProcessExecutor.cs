// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using System.Diagnostics;

namespace Metalama.Backstage.Infrastructure;

public interface IProcessExecutor : IBackstageService
{
    IProcess Start( ProcessStartInfo startInfo );
}