// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Utilities;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.CompileTime;

/// <summary>
/// A parameter, or a type parameter, of a template.
/// </summary>
/// <param name="TypeId">
/// The durable identifier of the type of the parameter, or <c>null</c> for a type parameter, which has no type of its
/// own.
/// </param>
/// <remarks>
/// <para>
/// The type is held as a <see cref="SerializableTypeId"/> rather than as an <see cref="ITypeSymbol"/>, and resolved
/// against the compilation of the caller by <see cref="GetTypeSymbol"/>. A parameter belongs to a
/// <see cref="TemplateClassMember"/>, which belongs to a template class, which belongs to
/// <c>AspectPipelineConfiguration</c>; and the configuration is reused at design time for every version of the project
/// and discarded only when compile-time code changes. A symbol of the source of a compilation would therefore keep the
/// version of the project in which the configuration was built alive for the whole editing session. See
/// <c>design-time-memory.md</c> and issue #1803.
/// </para>
/// <para>
/// The identifier is generated with the generic context included, because the type of a template parameter may be a type
/// parameter of the template itself, which cannot be resolved without knowing where it was declared.
/// </para>
/// </remarks>
[Durable]
internal sealed record TemplateClassMemberParameter(
    int SourceIndex,
    string Name,
    SerializableTypeId? TypeId,
    bool IsCompileTime,
    int? TemplateIndex,
    bool HasDefaultValue = false,
#pragma warning disable LAMA0870
    object? DefaultValue = null )
#pragma warning restore LAMA0870
{
    public TemplateClassMemberParameter( IParameterSymbol parameterSymbol, bool isCompileTime, int? templateIndex )
        : this(
            parameterSymbol.Ordinal,
            parameterSymbol.Name,
            parameterSymbol.Type.GetSerializableTypeId( includeGenericContext: true ),
            isCompileTime,
            templateIndex,
            parameterSymbol.HasExplicitDefaultValue,
            parameterSymbol.HasExplicitDefaultValue ? parameterSymbol.ExplicitDefaultValue : null ) { }

    /// <summary>
    /// Resolves the type of the parameter against a compilation, or returns <c>null</c> when the parameter has no type
    /// of its own.
    /// </summary>
    public ITypeSymbol? GetTypeSymbol( CompilationContext compilationContext )
        => this.TypeId == null ? null : compilationContext.SerializableTypeIdResolver.ResolveId( this.TypeId.Value );
}
