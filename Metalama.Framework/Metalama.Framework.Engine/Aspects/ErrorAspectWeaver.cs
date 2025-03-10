// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Engine.Diagnostics;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Aspects;

/// <summary>
/// An implementation of <see cref="IAspectWeaver"/> that represents a missing aspect weaver. Emits an error when used.
/// </summary>
internal sealed class ErrorAspectWeaver : IAspectWeaver
{
    private readonly AspectClass _aspectClass;

    public ErrorAspectWeaver( AspectClass aspectClass )
    {
        this._aspectClass = aspectClass;
    }

    public Task TransformAsync( AspectWeaverContext context )
    {
        context.ReportDiagnostic(
            GeneralDiagnosticDescriptors.CannotFindAspectWeaver.CreateRoslynDiagnostic(
                null,
                (this._aspectClass.WeaverType!, this._aspectClass.ShortName),
                this._aspectClass ) );

        return Task.CompletedTask;
    }
}