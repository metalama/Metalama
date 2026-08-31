// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Utilities;
using System;

namespace Metalama.Framework.Diagnostics;

internal sealed class SuppressionImpl( SuppressionDefinition definition, [Durable] Func<ISuppressibleDiagnostic, bool> filter ) : ISuppression
{
    public SuppressionDefinition Definition { get; } = definition;

#pragma warning disable LAMA0870
    public Func<ISuppressibleDiagnostic, bool> Filter { get; } = filter;
#pragma warning restore LAMA0870

    public override string ToString() => $"{this.Definition} with filter";
}