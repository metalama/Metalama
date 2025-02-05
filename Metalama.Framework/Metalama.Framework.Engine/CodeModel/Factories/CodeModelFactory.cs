// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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