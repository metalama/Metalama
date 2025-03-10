// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;

namespace Metalama.Framework.Engine.Transformations;

/// <summary>
/// Represents a transformation that introduces a declaration based on a <see cref="IDeclarationBuilder"/>, but does not
/// represent an override.
/// </summary>
internal interface IIntroduceDeclarationTransformation : ITransformation
{
    DeclarationBuilderData DeclarationBuilderData { get; }
}