// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.RunTime.Events;

namespace Metalama.Framework
{
    /// <summary>
    /// A type to be used as generic argument of <see cref="DiagnosticDefinition{T}"/> or <see cref="EventBroker{TDelegate,TArgs,TState}"/>
    /// to mean there is no value.
    /// </summary>
    [RunTimeOrCompileTime]
    public readonly struct None;
}