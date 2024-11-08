// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Metrics;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using SyntaxReference = Microsoft.CodeAnalysis.SyntaxReference;

namespace Metalama.Framework.Engine.CodeModel
{
    public abstract class BaseDeclaration : IDeclarationImpl
    {
        public abstract string ToDisplayString( CodeDisplayFormat? format = null, CodeDisplayContext? context = null );

        [PublicAPI]
        public abstract CompilationModel Compilation { get; }

        ICompilationElement? ICompilationElementImpl.Translate(
            CompilationModel newCompilation,
            IGenericContext? genericContext,
            Type? interfaceType )
            => this.Translate( newCompilation, genericContext );

        internal abstract ICompilationElement? Translate(
            CompilationModel newCompilation,
            IGenericContext? genericContext = null );

        ICompilation ICompilationElement.Compilation => this.Compilation;

        IRef<IDeclaration> IDeclaration.ToRef() => this.ToFullDeclarationRef();

        /// <summary>
        /// Returns an <see cref="IRef{T}"/> for the topmost interface supported by the type, i.e. not the base <see cref="IDeclaration"/>
        /// but <see cref="IMethod"/>, <see cref="INamedType"/>, ... 
        /// </summary>
        /// <returns></returns>
        private protected abstract IFullRef<IDeclaration> ToFullDeclarationRef();

        public SerializableDeclarationId ToSerializableId() => this.GetSerializableId();

        public abstract ImmutableArray<SyntaxReference> DeclaringSyntaxReferences { get; }

        public abstract bool CanBeInherited { get; }

        public abstract IEnumerable<IDeclaration> GetDerivedDeclarations( DerivedTypesOptions options = default );

        DeclarationImplementationKind IDeclarationImpl.ImplementationKind => this.ImplementationKind;

        internal abstract DeclarationImplementationKind ImplementationKind { get; }

        public abstract IAssembly DeclaringAssembly { get; }

        public abstract IDeclarationOrigin Origin { get; }

        public abstract IDeclaration? ContainingDeclaration { get; }

        public abstract IAttributeCollection Attributes { get; }

        public abstract DeclarationKind DeclarationKind { get; }

        public abstract bool IsImplicitlyDeclared { get; }

        [Memo]
        public int Depth => this.GetDepthImpl();

        public abstract bool BelongsToCurrentProject { get; }

        public abstract ImmutableArray<SourceReference> Sources { get; }

        IGenericContext IDeclaration.GenericContext => this.GenericContext;

        internal abstract GenericContext GenericContext { get; }

        public T GetMetric<T>()
            where T : IMetric
            => this.Compilation.MetricManager.GetMetric<T>( this );

        public abstract Location? DiagnosticLocation { get; }

        public abstract SyntaxTree? PrimarySyntaxTree { get; }

        /// <summary>
        /// This method is called from code model queries for which design-time cache invalidation is not implemented.
        /// </summary>
        protected static void OnUnsupportedDependency( string api )
        {
            UserCodeExecutionContext.CurrentOrNull?.OnUnsupportedDependency( api );
        }

        public abstract bool Equals( IDeclaration? other );

        public override bool Equals( object? obj ) => obj is BaseDeclaration baseDeclaration && this.Equals( baseDeclaration );

        public override int GetHashCode() => this.GetHashCodeCore();

        protected abstract int GetHashCodeCore();

        [Memo]
        private protected RefFactory RefFactory => this.Compilation.RefFactory;

        public override string ToString() => this.ToDisplayString( CodeDisplayFormat.ShortDiagnosticMessage );
    }
}