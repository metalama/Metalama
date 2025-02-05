// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Aspects;

internal interface ITemplateClassMemberBuilder : IProjectService
{
    bool TryGetMembers(
        TemplateClass templateClass,
        INamedTypeSymbol type,
        CompilationContext compilationContext,
        IDiagnosticAdder diagnosticAdder,
        out ImmutableDictionary<string, TemplateClassMember> members );
}