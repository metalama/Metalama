// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Utilities;

namespace Metalama.Framework.Engine.Utilities
{
    public static class EngineAssemblyMetadataReader
    {
        public static readonly AssemblyMetadataReader Instance = AssemblyMetadataReader.GetInstance( typeof(EngineAssemblyMetadataReader).Assembly );
    }
}