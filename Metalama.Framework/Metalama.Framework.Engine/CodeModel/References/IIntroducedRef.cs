// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;

namespace Metalama.Framework.Engine.CodeModel.References;

/// <summary>
/// A non-generic base interface for <see cref="IntroducedRef{T}"/>.
/// </summary>
internal interface IIntroducedRef : IFullRef
{
    DeclarationBuilderData BuilderData { get; }

    IFullRef? ReplacedDeclaration { get; }
}