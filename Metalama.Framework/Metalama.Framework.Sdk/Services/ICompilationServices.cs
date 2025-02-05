// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.CodeModel;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.Services;

[PublicAPI]
public interface ICompilationServices
{
    Compilation Compilation { get; }

    /// <summary>
    /// Gets a service able to map a system <see cref="Type"/> to a Roslyn <see cref="ITypeSymbol"/>.
    /// </summary>
    IReflectionMapper ReflectionMapper { get; }
}