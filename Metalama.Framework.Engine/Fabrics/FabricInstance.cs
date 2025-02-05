// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
            this.TargetDeclaration = targetDeclaration.ToRef();
            this.TargetDeclarationDepth = targetDeclaration.Depth;
        }

        public Fabric Fabric => this.Driver.Fabric;

        public FormattableString FormatPredecessor( ICompilation compilation ) => this.Driver.FormatPredecessor();

        public Location? GetDiagnosticLocation( Compilation compilation ) => this.Driver.DiagnosticLocation;

        int IAspectPredecessor.PredecessorDegree => 0;

        ImmutableArray<SyntaxTree> IAspectPredecessorImpl.PredecessorTreeClosure => ImmutableArray<SyntaxTree>.Empty;
    }
}