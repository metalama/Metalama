// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Infrastructure;
using System;
using System.Collections.Generic;

namespace Metalama.Backstage.Testing;

public class TestEnvironmentVariableProvider : IEnvironmentVariableProvider
{
    public Dictionary<string, string> Environment { get; } = new( StringComparer.OrdinalIgnoreCase );

    public string? GetEnvironmentVariable( string variable ) => this.Environment.ContainsKey( variable ) ? this.Environment[variable] : string.Empty;
}