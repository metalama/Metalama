// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Services;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.CompileTime.Serialization;

internal interface IDeserializationSurrogateProvider : IProjectService
{
    bool TryGetDeserializationSurrogate( string typeName, [NotNullWhen( true )] out Type? surrogateType );
}