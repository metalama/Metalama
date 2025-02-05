// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Services;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.AdditionalOutputs
{
    public interface IAdditionalOutputFileProvider : IProjectService
    {
        ImmutableArray<AdditionalCompilationOutputFile> GetAdditionalCompilationOutputFiles();
    }
}