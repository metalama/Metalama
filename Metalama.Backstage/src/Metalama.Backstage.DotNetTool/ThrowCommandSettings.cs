// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Commands;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Metalama.Backstage.DotNetTool;

/// <summary>
/// The distinct exceptions that <see cref="ThrowCommand"/> can throw. Each one is thrown from a different method, so
/// each is a different issue as far as exception reporting is concerned.
/// </summary>
internal enum ThrowCommandVariant
{
    A,
    B,
    C
}

[UsedImplicitly( ImplicitUseTargetFlags.WithMembers )]
internal class ThrowCommandSettings : BaseCommandSettings
{
    [Description( "The variant of the exception to throw. Each variant is reported as a distinct issue." )]
    [CommandOption( "--variant" )]
    public ThrowCommandVariant Variant { get; init; } = ThrowCommandVariant.A;
}
