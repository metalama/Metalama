// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Fabrics;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Fabrics
{
    internal sealed class FabricInstance : IFabricInstanceInternal, IAspectPredecessorImpl
    {
        private FabricDriver Driver { get; }

        public string FabricTypeFullName => this.Driver.FabricTypeFullName;

        IRef<IDeclaration> IAspectPredecessor.TargetDeclaration => this.TargetDeclaration;

        public ImmutableArray<AspectPredecessor> Predecessors => ImmutableArray<AspectPredecessor>.Empty;

        public IRef<IDeclaration> TargetDeclaration { get; }

        public int TargetDeclarationDepth { get; }

        public FabricInstance( FabricDriver driver, IDeclaration targetDeclaration )
        {
            this.Driver = driver;

            // Durable, and not merely a reference, because a fabric instance belongs to the pipeline configuration
            // through the amender of its fabric, and that configuration is long-lived by design, being reused across
            // keystrokes. A symbol-backed reference reaches the compilation through its factory, which would make the
            // configuration pin the version of the project it was built from for the whole editing session. See issue
            // #1799, and the same conversion applied for the same reason in FabricDriver.BaseAmender.
            this.TargetDeclaration = targetDeclaration.ToRef().ToDurable();

            this.TargetDeclarationDepth = targetDeclaration.Depth;
        }

        public Fabric Fabric => this.Driver.Fabric;

        public FormattableString FormatPredecessor( ICompilation compilation ) => this.Driver.FormatPredecessor();

        public Location? GetDiagnosticLocation( Compilation compilation ) => this.Driver.GetDiagnosticLocation( compilation );

        int IAspectPredecessor.PredecessorDegree => 0;

        ImmutableArray<SyntaxTree> IAspectPredecessorImpl.PredecessorTreeClosure => ImmutableArray<SyntaxTree>.Empty;
    }
}