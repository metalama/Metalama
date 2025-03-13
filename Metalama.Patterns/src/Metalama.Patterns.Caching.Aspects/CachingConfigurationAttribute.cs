// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;

namespace Metalama.Patterns.Caching.Aspects;

/// <summary>
/// Custom attribute that, when applied on a type, configures the <see cref="CacheAttribute"/> aspects applied to the methods of this type
/// or its derived types. When applied to an assembly, the <see cref="CachingConfigurationAttribute"/> custom attribute configures all methods
/// of the current assembly.
/// </summary>
/// <remarks>
/// <para>Any <see cref="CachingConfigurationAttribute"/> on the base class has always priority over a <see cref="CachingConfigurationAttribute"/>
/// on the assembly, even if the base class is in a different assembly.</para>
/// </remarks>
[PublicAPI]
[AttributeUsage( AttributeTargets.Class | AttributeTargets.Assembly )]
[RunTimeOrCompileTime]
public sealed class CachingConfigurationAttribute : CachingBaseAttribute;