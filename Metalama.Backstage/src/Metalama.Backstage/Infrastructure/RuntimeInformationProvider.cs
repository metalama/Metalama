// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Utilities;
using System.Runtime.InteropServices;

namespace Metalama.Backstage.Infrastructure;

/// <summary>
/// Wraps the <see cref="RuntimeInformation"/> class as an <see cref="IRuntimeInformation"/>.
/// </summary>
internal sealed class RuntimeInformationProvider : IRuntimeInformation
{
    public bool IsOSPlatform( OSPlatform osPlatform ) => RuntimeInformation.IsOSPlatform( osPlatform );

    public Architecture ProcessArchitecture => RuntimeInformation.ProcessArchitecture;

    public Architecture OSArchitecture => RuntimeInformation.OSArchitecture;

    public ProcessKind ProcessKind => ProcessUtilities.ProcessKind;
}
