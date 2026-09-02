// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Microsoft.CodeAnalysis;
using System;
using System.ComponentModel;

namespace Metalama.Framework.CompilerExtensions
{
    // ReSharper disable UnusedType.Global

    [Transformer]
    [DisplayName( "Metalama.Framework" )] // This name is used in telemetry. Changing it causes inconsistent data.
    public sealed class MetalamaSourceTransformer : ISourceTransformerWithServices
    {
        /// <summary>
        /// The descriptor of the diagnostic reported when the Roslyn version of the compiler is below the lowest
        /// supported one. At compile time, doing nothing would apply no aspect and report nothing, which is worse
        /// than failing the build, so this is an error and not a warning.
        /// </summary>
        private static readonly DiagnosticDescriptor _unsupportedRoslynVersion = new(
            "LAMA0087",
            "The Roslyn version of the compiler is not supported by Metalama",
            "Metalama requires Roslyn {0} or later, but the compiler is running Roslyn {1}, for which this build of "
            + "Metalama embeds no implementation. No aspect has been applied to this project. Upgrade the .NET SDK, "
            + "or use a version of Metalama that supports Roslyn {1}.",
            "Metalama.General",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true );

        /// <summary>
        /// The implementation, or <c>null</c> when the Roslyn version of the host is below the lowest supported one.
        /// </summary>
        private readonly ISourceTransformerWithServices? _impl;

        public MetalamaSourceTransformer()
        {
            ResourceExtractor.TryCreateInstance<ISourceTransformerWithServices>(
                "Metalama.Framework.Engine",
                "Metalama.Framework.Engine.Pipeline.SourceTransformer",
                out this._impl );
        }

        public IServiceProvider? InitializeServices( InitializeServicesContext context ) => this._impl?.InitializeServices( context );

        public void Execute( TransformerContext context )
        {
            if ( this._impl == null )
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        _unsupportedRoslynVersion,
                        Location.None,
                        RoslynVariantPolicy.MinimumSupportedRoslynVersion,
                        ResourceExtractor.HostRoslynVersion ) );

                return;
            }

            this._impl.Execute( context );
        }
    }
}