// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.DesignTime;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.CompilerExtensions
{
    // ReSharper disable UnusedType.Global

    [Generator( LanguageNames.CSharp )]
    public class MetalamaSourceGenerator : IIncrementalGenerator
    {
        private readonly IIncrementalGenerator? _impl;

        public MetalamaSourceGenerator()
        {
            if ( MetalamaCompilerInfo.IsActive )
            {
                return;
            }

            switch ( ProcessKindHelper.CurrentProcessKind )
            {
                case ProcessKind.Compiler:
                    //The service is not required.
                    break;

                case ProcessKind.DevEnv:
                    this._impl = (IIncrementalGenerator) ResourceExtractor.CreateInstance(
                        RoslynEntryPointTypeNames.DesignTimeAssemblyName,
                        RoslynEntryPointTypeNames.VsUserProcessSourceGenerator );

                    break;

                case ProcessKind.RoslynCodeAnalysisService:
                    this._impl = (IIncrementalGenerator) ResourceExtractor.CreateInstance(
                        RoslynEntryPointTypeNames.DesignTimeAssemblyName,
                        RoslynEntryPointTypeNames.VsAnalysisProcessSourceGenerator );

                    break;

                default:
                    this._impl = (IIncrementalGenerator) ResourceExtractor.CreateInstance(
                        RoslynEntryPointTypeNames.DesignTimeAssemblyName,
                        RoslynEntryPointTypeNames.AnalysisProcessSourceGenerator );

                    break;
            }
        }

        public void Initialize( IncrementalGeneratorInitializationContext context ) => this._impl?.Initialize( context );
    }
}