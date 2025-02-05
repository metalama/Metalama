// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;

namespace Metalama.Backstage.Infrastructure;

public interface IEnvironmentVariableProvider : IBackstageService
{
    string? GetEnvironmentVariable( string variable );
}