// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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