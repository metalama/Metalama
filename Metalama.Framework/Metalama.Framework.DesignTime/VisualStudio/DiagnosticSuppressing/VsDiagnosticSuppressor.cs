// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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