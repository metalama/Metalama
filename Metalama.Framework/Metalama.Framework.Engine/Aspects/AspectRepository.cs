// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Aspects;

/// <summary>
/// Abstractly exposes the internal <see cref="IAspectRepository"/> interface as a class that is public to the design-time projects.
/// </summary>
public abstract class AspectRepository : IAspectRepository
{
    public abstract AspectRepository WithAspectInstances( IEnumerable<IAspectInstance> aspectInstances, CompilationModel compilation );

    public abstract bool HasAspect( IDeclaration declaration, Type aspectType );

    public abstract IEnumerable<IAspectInstance> GetAspectInstances( IDeclaration declaration );
}