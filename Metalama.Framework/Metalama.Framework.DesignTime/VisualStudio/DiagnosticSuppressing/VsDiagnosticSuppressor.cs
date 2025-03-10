// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.DesignTime.DiagnosticSuppressing;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;

namespace Metalama.Framework.DesignTime.VisualStudio.DiagnosticSuppressing;

#pragma warning disable RS1001, RS1022

[UsedImplicitly]
[PublicAPI]
public sealed class VsDiagnosticSuppressor : TheDiagnosticSuppressor
{
    public VsDiagnosticSuppressor( ServiceProvider<IGlobalService> serviceProvider ) : base( serviceProvider ) { }

    public VsDiagnosticSuppressor() { }
}