// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.Aspects;

internal sealed class OtherTemplateClassFactory : TemplateClassFactory<OtherTemplateClass>
{
    protected override IEnumerable<TemplateClassData> GetFrameworkClasses() => [];

    protected override IEnumerable<string> GetTypeNames( CompileTimeProject project ) => project.OtherTemplateTypes;

    protected override bool TryCreate(
        ProjectServiceProvider serviceProvider,
        INamedTypeSymbol templateTypeSymbol,
        Type templateReflectionType,
        OtherTemplateClass? baseClass,
        CompileTimeProject? compileTimeProject,
        IDiagnosticAdder diagnosticAdder,
        ITemplateReflectionContext templateReflectionContext,
        [NotNullWhen( true )] out OtherTemplateClass? templateClass )
    {
        templateClass = new OtherTemplateClass(
            serviceProvider,
            templateReflectionContext,
            templateTypeSymbol,
            diagnosticAdder,
            baseClass,
            compileTimeProject.AssertNotNull() );

        return true;
    }

    public OtherTemplateClassFactory( CompilationContext compilationContext ) : base( compilationContext ) { }
}