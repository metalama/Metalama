// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Options;

namespace Metalama.Framework.Engine.Linking
{
    /// <summary>
    /// Output of the linker analysis.
    /// </summary>
    internal sealed class LinkerAnalysisStepOutput
    {
        public LinkerAnalysisStepOutput(
            UserDiagnosticSink diagnosticSink,
            CompilationModel sourceCompilationModel,
            PartialCompilation intermediateCompilation,
            LinkerInjectionRegistry injectionRegistry,
            LinkerLateTransformationRegistry linkerLateTransformationRegistry,
            LinkerAnalysisRegistry analysisRegistry,
            IProjectOptions? projectOptions )
        {
            this.DiagnosticSink = diagnosticSink;
            this.SourceCompilationModel = sourceCompilationModel;
            this.IntermediateCompilation = intermediateCompilation;
            this.InjectionRegistry = injectionRegistry;
            this.AnalysisRegistry = analysisRegistry;
            this.LateTransformationRegistry = linkerLateTransformationRegistry;
            this.ProjectOptions = projectOptions;
        }

        /// <summary>
        /// Gets diagnostic sink.
        /// </summary>
        public UserDiagnosticSink DiagnosticSink { get; }

        public CompilationModel SourceCompilationModel { get; }
        
        /// <summary>
        /// Gets the intermediate compilation (produced in injection step).
        /// </summary>
        public PartialCompilation IntermediateCompilation { get; }

        /// <summary>
        /// Gets the injection registry.
        /// </summary>
        public LinkerInjectionRegistry InjectionRegistry { get; }

        /// <summary>
        /// Gets the registry of late transformations that are performed during linking.
        /// </summary>
        public LinkerLateTransformationRegistry LateTransformationRegistry { get; }

        /// <summary>
        /// Gets the analysis registry.
        /// </summary>
        public LinkerAnalysisRegistry AnalysisRegistry { get; }

        /// <summary>
        /// Gets project options.
        /// </summary>
        public IProjectOptions? ProjectOptions { get; }
    }
}