// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Backstage.Infrastructure;

public class EnvironmentVariableProvider : IEnvironmentVariableProvider
{
    public string? GetEnvironmentVariable( string variable ) => Environment.GetEnvironmentVariable( variable, EnvironmentVariableTarget.Process );
}