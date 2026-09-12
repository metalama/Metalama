// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

/// <summary>
/// Implemented by the builder of a type kind whose declaration carries members that the advice registers in the code
/// model without emitting them.
/// </summary>
/// <remarks>
/// <para>
/// Section 4.2 of <c>Metalama.Framework/docs/introducing-types.md</c> states the rule: Metalama emits the
/// declaration of the type, the compiler synthesizes the members from it, and those members must nevertheless exist
/// in the code model, because the pipeline never re-reads the final model from Roslyn. The advice registers what
/// this method returns and emits none of it.
/// </para>
/// <para>
/// An enum is the one kind whose members are not synthesized by the compiler: they are written by the aspect and
/// emitted inside the declaration. It implements this interface nevertheless, because the registration is the same
/// and the advice has one path rather than one per kind.
/// </para>
/// </remarks>
internal interface ITypeBuilderWithSynthesizedMembers
{
    /// <summary>
    /// Gets the immutable data of every member that the advice registers in the code model without emitting it. The
    /// builder must be frozen before this method is called, because the data of a builder exists only from that
    /// point.
    /// </summary>
    IEnumerable<NamedDeclarationBuilderData> GetSynthesizedMemberData();
}
