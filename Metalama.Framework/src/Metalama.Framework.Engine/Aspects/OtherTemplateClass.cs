// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Fabrics;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.Aspects;

/// <summary>
/// Represents a template class implementing <see cref="ITemplateProvider"/> but neither <see cref="IAspect"/> nor <see cref="Fabric"/>.
/// </summary>
internal sealed class OtherTemplateClass : TemplateClass
{
    public OtherTemplateClass(
        ProjectServiceProvider serviceProvider,
        ITemplateReflectionContext compilationContext,
        INamedTypeSymbol typeSymbol,
        IDiagnosticAdder diagnosticAdder,
        OtherTemplateClass? baseClass,
        CompileTimeProject project )
        : base( serviceProvider, compilationContext, typeSymbol, diagnosticAdder, baseClass, typeSymbol.Name )
    {
        this.Type = project.GetType( typeSymbol.GetReflectionFullName().AssertNotNull() );
    }

    internal override Type Type { get; }

    public override string FullName => this.Type.FullName!;
}