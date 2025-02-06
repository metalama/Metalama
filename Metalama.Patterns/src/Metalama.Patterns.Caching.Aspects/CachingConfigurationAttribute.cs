// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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