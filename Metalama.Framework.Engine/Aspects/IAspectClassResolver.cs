// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.Aspects;

public interface IAspectClassResolver
{
    bool TryGetAspectClass( Type aspectType, [NotNullWhen( true )] out IAspectClass? aspectClass );
}