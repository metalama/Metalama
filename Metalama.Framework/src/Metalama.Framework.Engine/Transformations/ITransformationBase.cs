// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Introspection;
using System;

namespace Metalama.Framework.Engine.Transformations;

public interface ITransformationBase
{
    IAspectClass AspectClass { get; }

    IRef<IDeclaration> TargetDeclaration { get; }

    IntrospectionTransformationKind TransformationKind { get; }

    /// <summary>
    /// Gets a human-readable description of the transformation, to be displayed in the UI.
    /// </summary>
    FormattableString? ToDisplayString();
}