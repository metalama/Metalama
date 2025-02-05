// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Services;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.CompileTime.Serialization;

internal interface IDeserializationSurrogateProvider : IProjectService
{
    bool TryGetDeserializationSurrogate( string typeName, [NotNullWhen( true )] out Type? surrogateType );
}