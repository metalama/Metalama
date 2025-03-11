// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.Fabrics;

/// <summary>
/// An implementation of <see cref="TemplateClass"/> that represents a fabric class.
/// </summary>
internal sealed class FabricTemplateClass : TemplateClass
{
    public FabricDriver Driver { get; }

    public FabricTemplateClass(
        ProjectServiceProvider serviceProvider,
        FabricDriver fabricDriver,
        ITemplateReflectionContext templateReflectionContext,
        IDiagnosticAdder diagnosticAdder,
        TemplateClass? baseClass ) :
        base(
            serviceProvider,
            templateReflectionContext,
            (INamedTypeSymbol) fabricDriver.FabricTypeSymbolId.Resolve( templateReflectionContext.Compilation ).AssertSymbolNotNull(),
            diagnosticAdder,
            baseClass,
            fabricDriver.FabricTypeShortName )
    {
        this.Driver = fabricDriver;
    }

    internal override Type Type => this.Driver.Fabric.GetType();

    public override string FullName => this.Driver.FabricTypeFullName;
}