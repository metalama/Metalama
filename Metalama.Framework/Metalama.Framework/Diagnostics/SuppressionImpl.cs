// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Diagnostics;

internal sealed class SuppressionImpl( SuppressionDefinition definition, Func<ISuppressibleDiagnostic, bool> filter ) : ISuppression
{
    public SuppressionDefinition Definition { get; } = definition;

    public Func<ISuppressibleDiagnostic, bool> Filter { get; } = filter;

    public override string ToString() => $"{this.Definition} with filter";
}