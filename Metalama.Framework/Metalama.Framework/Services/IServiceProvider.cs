// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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