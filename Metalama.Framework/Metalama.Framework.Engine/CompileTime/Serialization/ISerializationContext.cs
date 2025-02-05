// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Services;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CompileTime.Serialization;

internal interface ISerializationContext
{
    CompilationContext CompilationContext { get; }

    Dictionary<string, object?> ContextProperties { get; }
}