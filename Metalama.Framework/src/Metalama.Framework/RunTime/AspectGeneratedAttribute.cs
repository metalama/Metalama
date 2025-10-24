// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.RunTime;

/// <summary>
/// A custom attribute added to introduced parameters.
/// </summary>
/// <remarks>This attribute allows to construct the pre-transformation <see cref="SerializableDeclarationId"/> of the member,
/// before any parameter was added. It is added when the constructor can be called from an external assembly.</remarks>
[AttributeUsage( AttributeTargets.Parameter )]
public sealed class AspectGeneratedAttribute : Attribute;