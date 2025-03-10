// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.Infrastructure;

internal sealed class RecoverableExceptionService : IRecoverableExceptionService
{
    public RecoverableExceptionService( IServiceProvider serviceProvider )
    {
        var environmentVariables = serviceProvider.GetRequiredBackstageService<IEnvironmentVariableProvider>();
        this.CanIgnore = environmentVariables.GetEnvironmentVariable( "IS_POSTSHARP_OWNED" ) == null;
    }

    public bool CanIgnore { get; }
}