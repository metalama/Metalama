// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Compiler;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.CodeModel.Factories
{
    [UsedImplicitly] // Reference not detected.
    public static class CodeModelFactory
    {
        [UsedImplicitly]
        public static ICompilation CreateCompilation(
            Compilation compilation,
            ProjectServiceProvider serviceProvider,
            ImmutableArray<ManagedResource> resources = default )
        {
            var partialCompilation = PartialCompilation.CreateComplete( compilation, resources );
            var projectModel = new ProjectModel( compilation, serviceProvider );

            return CompilationModel.CreateInitialInstance( projectModel, partialCompilation );
        }
    }
}