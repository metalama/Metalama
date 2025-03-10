// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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