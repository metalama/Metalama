// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Spectre.Console.Cli;
using System;

namespace Metalama.Backstage.Desktop.Windows.Commands;

// ReSharper disable once NotAccessedPositionalProperty.Global
public record ExtendedCommandContext( CommandContext CommandContext, IServiceProvider ServiceProvider, ILogger Logger );