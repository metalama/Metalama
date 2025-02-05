// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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