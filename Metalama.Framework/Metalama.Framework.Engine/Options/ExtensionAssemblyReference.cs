// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.Options;

/// <summary>
/// Represent a framework-specific assembly reference including its target framework.
/// </summary>
public sealed record ExtensionAssemblyReference( string Path, string? TargetFramework = null );