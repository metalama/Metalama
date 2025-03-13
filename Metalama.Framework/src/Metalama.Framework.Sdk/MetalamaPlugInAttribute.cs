// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Engine;

/// <summary>
/// Custom attribute that, when applied to a class, means that an instance
/// of this class must be created and is exposed to Metalama.
/// This instance can then be available in Metalama as a service, and exposed to <see cref="IServiceProvider"/>.
/// </summary>
[AttributeUsage( AttributeTargets.Class )]
[CompileTime]
[PublicAPI]
public sealed class MetalamaPlugInAttribute : Attribute;