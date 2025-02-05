// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Engine.Options;

/// <summary>
/// Represent a framework-specific assembly reference including its target framework.
/// </summary>
public sealed record ExtensionAssemblyReference( string Path, string? TargetFramework = null );