// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Introspection;
using System;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

/// <summary>
/// A transformation that replaces the type of an already-introduced constructor parameter.
/// Inherits <see cref="IntroduceParameterTransformation"/> so the linker's parameter syntax
/// emission path can treat it uniformly; during <c>Sort()</c> in MemberLevelTransformations,
/// it supersedes the original <see cref="IntroduceParameterTransformation"/> at the same index.
/// </summary>
internal sealed class ReplaceParameterTransformation : IntroduceParameterTransformation
{
    /// <summary>
    /// Gets the index of the parameter being replaced in the target constructor.
    /// </summary>
    public int ReplacedParameterIndex { get; }

    public override bool IsReplacement => true;

    public ReplaceParameterTransformation(
        AspectLayerInstance aspectLayerInstance,
        ParameterBuilderData newParameter,
        int replacedParameterIndex ) : base(
        aspectLayerInstance,
        newParameter )
    {
        this.ReplacedParameterIndex = replacedParameterIndex;
    }

    public override IntrospectionTransformationKind TransformationKind => IntrospectionTransformationKind.IntroduceParameter;

    public override FormattableString ToDisplayString()
    {
        var containingDeclarationDefinition = this.Parameter.ContainingDeclaration.Definition;

        return
            $"Replace parameter type at index {this.ReplacedParameterIndex} in {containingDeclarationDefinition.DeclarationKind.ToDisplayString()} '{containingDeclarationDefinition}'.";
    }
}