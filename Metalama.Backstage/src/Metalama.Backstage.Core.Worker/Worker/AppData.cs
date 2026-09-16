// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.Extensions.DependencyInjection;
using System;

namespace Metalama.Backstage.Worker;

/// <summary>
/// The data that the commands of the worker receive.
/// </summary>
/// <param name="ServiceCollection">The registrations of the Backstage services, which the web server copies into its own container.</param>
/// <param name="ServiceProvider">The Backstage services of the worker process.</param>
internal sealed record AppData( ServiceCollection ServiceCollection, IServiceProvider ServiceProvider );
