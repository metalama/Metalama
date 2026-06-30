// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Infrastructure;
using System.Runtime.InteropServices;

namespace Metalama.Backstage.Testing;

public sealed class TestRuntimeInformation : IRuntimeInformation
{
    private readonly RuntimeInformationProvider _defaultValues = new();

    public OSPlatform? Platform { get; set; }

    public Architecture? TestProcessArchitecture { get; set; }

    // ReSharper disable once InconsistentNaming
    public Architecture? TestOSArchitecture { get; set; }

    public ProcessKind? TestProcessKind { get; set; }

    public bool IsOSPlatform( OSPlatform osPlatform )
    {
        if ( this.Platform == null )
        {
            return this._defaultValues.IsOSPlatform( osPlatform );
        }

        return this.Platform.Value.Equals( osPlatform );
    }

    public Architecture ProcessArchitecture => this.TestProcessArchitecture ?? this._defaultValues.ProcessArchitecture;

    public Architecture OSArchitecture => this.TestOSArchitecture ?? this._defaultValues.OSArchitecture;

    public ProcessKind ProcessKind => this.TestProcessKind ?? this._defaultValues.ProcessKind;
}
