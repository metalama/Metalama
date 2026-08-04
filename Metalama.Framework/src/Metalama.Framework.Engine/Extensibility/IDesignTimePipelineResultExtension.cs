// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.ReferenceGraph;
using Metalama.Framework.Engine.Utilities.Roslyn;

namespace Metalama.Framework.Engine.Extensibility;

/// <summary>
/// Represents the design-time form of a contributor produced by an extension, as returned by
/// <see cref="ITransitivePipelineContributor.ToDesignTime"/>.
/// </summary>
/// <remarks>
/// <para>
/// An implementation must be durable, that is, it must hold no reference to a <c>Compilation</c>, a
/// <c>SyntaxTree</c>, an <c>ISymbol</c>, a <c>CompilationModel</c>, a declaration of the code model, or a
/// non-durable <see cref="Metalama.Framework.Code.IRef{T}"/>, and none to any object that reaches one. This is a
/// requirement, not a recommendation: the design-time pipeline stores these objects for far longer than the run that
/// produced them.
/// </para>
/// <para>
/// The pipeline files each of them in the result of the syntax tree it belongs to, and it carries the result of every
/// file that a run did not analyse forward to the next version of the project unchanged. It goes further: when a
/// later run produces a contributor for a file that is not dirty, it discards the new instance so that the earlier one
/// survives. An instance filed under a file the user does not edit therefore lives for the whole editing session, and
/// anything bound to a compilation that it holds keeps that entire version of the project alive.
/// </para>
/// <para>
/// Use <see cref="Metalama.Framework.Engine.Utilities.Roslyn.SymbolDictionaryKey.CreatePersistentKey"/> to refer to a
/// symbol, and <c>IRef.ToDurable()</c> to refer to a declaration. Both keep only a serializable identifier. See
/// <c>Metalama.Framework/docs/design-time-memory.md</c>, and issue #1799 for what the absence of this requirement
/// cost.
/// </para>
/// </remarks>
public interface IDesignTimePipelineResultExtension : IContributor
{
    ITransitiveAspectsManifestExtension ToTransitiveAspectManifestExtension();
}

public interface IDesignTimeValidatorExtension : IDesignTimePipelineResultExtension
{
    ReferenceIndexerRequirements? ReferenceIndexerRequirements { get; }

    SymbolDictionaryKey ValidatedDeclaration { get; }
}