// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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