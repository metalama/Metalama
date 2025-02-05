// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Project;
using Microsoft.CodeAnalysis;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.Fabrics
{
    /// <summary>
    /// Implementation of <see cref="FabricAspect{T}"/> for namespace-level fabrics.
    /// </summary>
    internal sealed class NamespaceFabricDriver : StaticFabricDriver
    {
        private readonly string _targetNamespace;

        private NamespaceFabricDriver( CreationData creationData ) :
            base( creationData )
        {
            this._targetNamespace = creationData.FabricType.ContainingNamespace.GetFullName().AssertNotNull();
        }

        public static NamespaceFabricDriver Create(
            FabricManager fabricManager,
            CompileTimeProject compileTimeProject,
            Fabric fabric,
            Compilation runTimeCompilation )
            => new( GetCreationData( fabricManager, compileTimeProject, fabric, runTimeCompilation ) );

        public override FabricKind Kind => FabricKind.Namespace;

        public override FormattableString FormatPredecessor() => $"namespace fabric '{this.Fabric.GetType()}' on '{this._targetNamespace}'";

        public override bool TryExecute(
            IProject project,
            CompilationModel compilation,
            IDiagnosticAdder diagnosticAdder,
            [NotNullWhen( true )] out StaticFabricResult? result )
        {
            var namespaceSymbol = compilation.RoslynCompilation.SourceModule.GlobalNamespace.GetDescendant( this._targetNamespace );

            if ( namespaceSymbol == null ||
                 (compilation.PartialCompilation.IsPartial && !compilation.PartialCompilation.Namespaces.Contains( namespaceSymbol )) )
            {
                result = StaticFabricResult.Empty;

                return true;
            }

            var executionContext = UserCodeExecutionContext.CreateInstance(
                this.FabricManager.ServiceProvider,
                UserCodeDescription.Create( "calling the AmendNamespace method for the fabric {0}", this.Fabric.GetType() ),
                compilation,
                diagnostics: diagnosticAdder );

            var amender = new Amender(
                project,
                this.FabricManager,
                new FabricInstance( this, compilation.Factory.GetNamespace( namespaceSymbol ) ),
                namespaceSymbol.GetFullName() ?? "",
                executionContext );

            if ( !this.FabricManager.UserCodeInvoker.TryInvoke( () => ((NamespaceFabric) this.Fabric).AmendNamespace( amender ), executionContext ) )
            {
                result = null;

                return false;
            }

            // TODO: Exception handling.

            result = amender.ToResult();

            return true;
        }

        private sealed class Amender : StaticAmender<INamespace>, INamespaceAmender
        {
            public Amender(
                IProject project,
                FabricManager fabricManager,
                FabricInstance fabricInstance,
                string ns,
                UserCodeExecutionContext userCodeExecutionContext ) : base(
                project,
                fabricManager,
                fabricInstance,
                fabricInstance.TargetDeclaration.As<INamespace>(),
                ns,
                userCodeExecutionContext ) { }

            string INamespaceAmender.Namespace => this.Namespace.AssertNotNull();
        }
    }
}