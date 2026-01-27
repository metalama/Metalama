// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Transformations;

/// <summary>
/// Represents any transformation.
/// </summary>
internal interface ITransformation : ITransformationBase
{
    AspectLayerInstance AspectLayerInstance { get; }

    // Resharper disable once UnusedMemberInSuper.Global
    AspectLayerId AspectLayerId { get; }

    IAspectInstanceInternal AspectInstance { get; }

    int OrderWithinPipelineStepAndTypeAndAspectInstance { get; set; }

    int OrderWithinPipelineStepAndType { get; set; }

    int OrderWithinPipeline { get; set; }

    TransformationObservability Observability { get; }

    /// <summary>
    /// Gets implicit declarations that are generated as a side effect of this transformation.
    /// These declarations are added to the code model but do not have corresponding syntax transformations.
    /// Used primarily for extension member implicit implementations.
    /// </summary>
    IEnumerable<DeclarationBuilderData> GetImplicitDeclarations();
}