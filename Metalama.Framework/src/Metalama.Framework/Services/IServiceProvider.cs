// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

// ReSharper disable TypeParameterCanBeVariant

namespace Metalama.Framework.Services;

/// <summary>
/// A strongly-typed variant of <see cref="IServiceProvider"/> that returns services for a given scope.
/// </summary>
/// <typeparam name="TBase">The base interface for the services in the scope.</typeparam>
/// <remarks>
/// The generic interface is intentionally not variant.
/// </remarks>
[CompileTime]
public interface IServiceProvider<TBase> : IServiceProvider
{
    T? GetService<T>()
        where T : class, TBase;
}