// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.CompileTime;

internal sealed record TemplateClassMemberParameter(
    int SourceIndex,
    string Name,
    ITypeSymbol? Type,
    bool IsCompileTime,
    int? TemplateIndex,
    bool HasDefaultValue = false,
    object? DefaultValue = null )
{
    public TemplateClassMemberParameter( IParameterSymbol parameterSymbol, bool isCompileTime, int? templateIndex )
        : this(
            parameterSymbol.Ordinal,
            parameterSymbol.Name,
            parameterSymbol.Type,
            isCompileTime,
            templateIndex,
            parameterSymbol.HasExplicitDefaultValue,
            parameterSymbol.HasExplicitDefaultValue ? parameterSymbol.ExplicitDefaultValue : null ) { }
}