// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;

namespace Metalama.Backstage.Infrastructure
{
    public interface IPlatformInfo : IBackstageService
    {
        string? DotNetSdkDirectory { get; }

        string DotNetExePath { get; }

        string? DotNetSdkVersion { get; }
    }
}