// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Fabrics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Project;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Queries
{
    /// <summary>
    /// An implementation of <see cref="IQuery{TDeclaration}"/>, which offers a fluent
    /// API to programmatically add children aspects.
    /// </summary>
    internal abstract class Query<TDeclaration, TTag> : IQueryImpl<TDeclaration>, ITaggedQuery<TDeclaration, TTag>
        where TDeclaration : class, IDeclaration
    {
        public abstract IQueryOwner Owner { get; }

        private readonly CompilationModelVersion _compilationModelVersion;
        private readonly Func<Func<TDeclaration, TTag, QueryExecutionContext, Task>, QueryExecutionContext, Task> _adder;
        private readonly IConcurrentTaskRunner _concurrentTaskRunner;

        // We track the number of children to know if we must cache results.
        private int _childrenCount;

        internal Query(
            ProjectServiceProvider serviceProvider,
            IRef<IDeclaration> containingDeclaration,
            CompilationModelVersion compilationModelVersion,
            Func<Func<TDeclaration, TTag, QueryExecutionContext, Task>, QueryExecutionContext, Task> addTargets )
        {
            this._concurrentTaskRunner = serviceProvider.GetRequiredService<IConcurrentTaskRunner>();
            this.OriginatingDeclaration = containingDeclaration;
            this._compilationModelVersion = compilationModelVersion;
            this._adder = addTargets;
        }

        public IProject Project => this.Owner.Project;

        public string? OriginatingNamespace => this.Owner.Namespace;

        public IRef<IDeclaration> OriginatingDeclaration { get; }

        protected virtual bool ShouldCache => this._childrenCount > 1;

        public void OnChildAdded()
        {
            this._childrenCount++;
        }

        private Query<TChildDeclaration, TChildTag> AddChild<TChildDeclaration, TChildTag>( Query<TChildDeclaration, TChildTag> child )
            where TChildDeclaration : class, IDeclaration
        {
            this._childrenCount++;

            return child;
        }

        ITaggedQuery<TMember, TTag> ITaggedQuery<TDeclaration, TTag>.SelectMany<TMember>( Func<TDeclaration, IEnumerable<TMember>> selector )
            => this.SelectMany( ( declaration, _ ) => selector( declaration ) );

        ITaggedQuery<TMember, TTag> ITaggedQuery<TDeclaration, TTag>.Select<TMember>( Func<TDeclaration, TMember> selector )
            => this.Select( ( declaration, _ ) => selector( declaration ) );

        IQuery<INamedType> IQuery<TDeclaration>.SelectTypes( bool includeNestedTypes ) => this.SelectTypes( includeNestedTypes );

        ITaggedQuery<INamedType, TTag> ITaggedQuery<TDeclaration, TTag>.SelectTypesDerivedFrom( Type baseType, DerivedTypesOptions options )
            => this.SelectTypesDerivedFromCore( c => (INamedType) c.Factory.GetTypeByReflectionType( baseType ), options );

        ITaggedQuery<INamedType, TTag> ITaggedQuery<TDeclaration, TTag>.SelectTypesDerivedFrom(
            INamedType baseType,
            DerivedTypesOptions options )
            => this.SelectTypesDerivedFromCore( _ => baseType, options );

        IQuery<INamedType> IQuery<TDeclaration>.SelectTypesDerivedFrom( Type baseType, DerivedTypesOptions options )
            => this.SelectTypesDerivedFromCore( c => (INamedType) c.Factory.GetTypeByReflectionType( baseType ), options );

        IQuery<INamedType> IQuery<TDeclaration>.SelectTypesDerivedFrom( INamedType baseType, DerivedTypesOptions options )
            => this.SelectTypesDerivedFromCore( _ => baseType, options );

        private ITaggedQuery<INamedType, TTag> SelectTypesDerivedFromCore( Func<CompilationModel, INamedType> getBaseType, DerivedTypesOptions options )
            => this.AddChild(
                new ChildQuery<INamedType, TTag>(
                    this.OriginatingDeclaration,
                    this.Owner,
                    this._compilationModelVersion,
                    ( action, context ) => this.InvokeAdderAsync(
                        context,
                        ( declaration, tag, context2 ) =>
                        {
                            var baseType = getBaseType( declaration.GetCompilationModel() );

                            if ( declaration is CompilationModel compilation )
                            {
                                var types = compilation.GetDerivedTypes( baseType, options );

                                return this._concurrentTaskRunner.RunConcurrentlyAsync(
                                    types,
                                    child => action( child, tag, context ),
                                    context2.CancellationToken );
                            }
                            else if ( options != DerivedTypesOptions.Default )
                            {
                                throw new NotImplementedException(
                                    $"Non-default DerivedTypesOptions are only implemented for ICompilation but was used with a {declaration.DeclarationKind.ToDisplayString()}." );
                            }
                            else
                            {
                                IEnumerable<INamedType> types;

                                switch ( declaration )
                                {
                                    case INamespace ns:
                                        types = ns.DescendantsAndSelf().SelectMany( x => x.Types ).SelectManyRecursive( x => x.NestedTypesAndSelf() );

                                        break;

                                    case INamedType namedType:
                                        types = namedType.NestedTypesAndSelf();

                                        break;

                                    case var _ when declaration.GetTopmostNamedType() is { } topmostType:
                                        types = topmostType.NestedTypesAndSelf();

                                        break;

                                    default:
                                        return Task.CompletedTask;
                                }

                                return this._concurrentTaskRunner.RunConcurrentlyAsync(
                                    types,
                                    child =>
                                    {
                                        if ( child.IsConvertibleTo( baseType ) )
                                        {
                                            return action( child, tag, context );
                                        }
                                        else
                                        {
                                            return Task.CompletedTask;
                                        }
                                    },
                                    context2.CancellationToken );
                            }
                        } ) ) );

        ITaggedQuery<TDeclaration, TTag> ITaggedQuery<TDeclaration, TTag>.Where( Func<TDeclaration, bool> predicate )
            => this.Where( ( declaration, _ ) => predicate( declaration ) );

        ITaggedQuery<TOut, TTag> ITaggedQuery<TDeclaration, TTag>.OfType<TOut>() => this.OfType<TOut>();

        IQuery<TOut> IQuery<TDeclaration>.OfType<TOut>() => this.OfType<TOut>();

        public IReadOnlyCollection<TDeclaration> ToCollection( ICompilation? compilation )
        {
            var compilationModel = (CompilationModel?) compilation ?? UserCodeExecutionContext.Current.Compilation.AssertNotNull();

            if ( compilationModel.IsPartial )
            {
                throw new InvalidOperationException( "This method cannot be used with a partial compilation (typically at design time." );
            }

            var bag = new ConcurrentQueue<TDeclaration>();

            this.Owner.ServiceProvider.Global.GetRequiredService<ITaskRunner>()
                .RunSynchronously(
                    () => this.InvokeAdderAsync(
                        new QueryExecutionContext(
                            compilationModel,
                            NullDiagnosticAdder.Instance,
                            this.Owner.UserCodeInvoker,
                            this.Owner.UserCodeExecutionContext,
                            CancellationToken.None ),
                        ( declaration, _, _ ) =>
                        {
                            bag.Enqueue( declaration );

                            return Task.CompletedTask;
                        } ) );

            return bag;
        }

        public ITaggedQuery<TDeclaration, TTag1> Tag<TTag1>( Func<TDeclaration, TTag1> getTag ) => this.WithTag( getTag );

        ITaggedQuery<TDeclaration, TTag1> IQuery<TDeclaration>.WithTag<TTag1>( Func<TDeclaration, TTag1> getTag )
            => this.WithTag( ( declaration, _ ) => getTag( declaration ) );

        public ITaggedQuery<TDeclaration, TNewTag> WithTag<TNewTag>( Func<TDeclaration, TNewTag> getTag )
            => this.WithTag( ( declaration, _ ) => getTag( declaration ) );

        ITaggedQuery<TDeclaration, TNewTag> ITaggedQuery<TDeclaration, TTag>.Tag<TNewTag>( Func<TDeclaration, TNewTag> getTag )
            => this.WithTag( ( declaration, _ ) => getTag( declaration ) );

        ITaggedQuery<TDeclaration, TNewTag> ITaggedQuery<TDeclaration, TTag>.Tag<TNewTag>( Func<TDeclaration, TTag, TNewTag> getTag ) => this.WithTag( getTag );

        public ITaggedQuery<TDeclaration, TNewTag> WithTag<TNewTag>( Func<TDeclaration, TTag, TNewTag> getTag )
            => this.AddChild(
                new ChildQuery<TDeclaration, TNewTag>(
                    this.OriginatingDeclaration,
                    this.Owner,
                    this._compilationModelVersion,
                    ( action, context ) => this.InvokeAdderAsync(
                        context,
                        ( declaration, tag, context2 ) =>
                        {
                            var newTag = getTag( declaration, tag );

                            return action( declaration, newTag, context2 );
                        } ) ) );

        public ITaggedQuery<TMember, TTag> SelectMany<TMember>( Func<TDeclaration, TTag, IEnumerable<TMember>> selector )
            where TMember : class, IDeclaration
            => this.AddChild(
                new ChildQuery<TMember, TTag>(
                    this.OriginatingDeclaration,
                    this.Owner,
                    this._compilationModelVersion,
                    ( action, context ) => this.InvokeAdderAsync(
                        context,
                        ( declaration, tag, context2 ) =>
                        {
                            var children = selector( declaration, tag );

                            return this._concurrentTaskRunner.RunConcurrentlyAsync(
                                children,
                                child => action( child, tag, context ),
                                context2.CancellationToken );
                        } ) ) );

        public ITaggedQuery<TMember, TTag> Select<TMember>( Func<TDeclaration, TTag, TMember> selector )
            where TMember : class, IDeclaration
            => this.AddChild(
                new ChildQuery<TMember, TTag>(
                    this.OriginatingDeclaration,
                    this.Owner,
                    this._compilationModelVersion,
                    ( action, context ) => this.InvokeAdderAsync(
                        context,
                        ( declaration, tag, context2 ) => action( selector( declaration, tag ), tag, context2 ) ) ) );

        public ITaggedQuery<INamedType, TTag> SelectTypes( bool includeNestedTypes = true )
            => this.AddChild(
                new ChildQuery<INamedType, TTag>(
                    this.OriginatingDeclaration,
                    this.Owner,
                    this._compilationModelVersion,
                    ( action, context ) => this.InvokeAdderAsync(
                        context,
                        ( declaration, tag, context2 ) =>
                        {
                            IEnumerable<INamedType> types;

                            if ( declaration is IAssembly assembly )
                            {
                                types = includeNestedTypes ? assembly.AllTypes : assembly.Types;
                            }
                            else
                            {
                                switch ( declaration )
                                {
                                    case INamespace ns:
                                        types = ns.DescendantsAndSelf().SelectMany( x => x.Types );

                                        break;

                                    case INamedType type:
                                        types = [type];

                                        break;

                                    case var _ when declaration.GetTopmostNamedType() is { } topmostType:
                                        types = [topmostType];

                                        break;

                                    default:
                                        return Task.CompletedTask;
                                }

                                if ( includeNestedTypes )
                                {
                                    types = types.SelectMany( t => t.NestedTypesAndSelf() );
                                }
                            }

                            return this._concurrentTaskRunner.RunConcurrentlyAsync(
                                types,
                                child => action( child, tag, context ),
                                context2.CancellationToken );
                        } ) ) );

        public ITaggedQuery<TDeclaration, TTag> Where( Func<TDeclaration, TTag, bool> predicate )
            => this.AddChild(
                new ChildQuery<TDeclaration, TTag>(
                    this.OriginatingDeclaration,
                    this.Owner,
                    this._compilationModelVersion,
                    ( action, context ) => this.InvokeAdderAsync(
                        context,
                        ( declaration, tag, context2 ) =>
                        {
                            if ( predicate( declaration, tag ) )
                            {
                                return action( declaration, tag, context2 );
                            }
                            else
                            {
                                return Task.CompletedTask;
                            }
                        } ) ) );

        private ITaggedQuery<TOut, TTag> OfType<TOut>()
            where TOut : class, IDeclaration
            => this.AddChild(
                new ChildQuery<TOut, TTag>(
                    this.OriginatingDeclaration,
                    this.Owner,
                    this._compilationModelVersion,
                    ( action, context ) => this.InvokeAdderAsync(
                        context,
                        ( declaration, tag, context2 ) =>
                        {
                            if ( declaration is TOut outDeclaration )
                            {
                                return action( outDeclaration, tag, context2 );
                            }
                            else
                            {
                                return Task.CompletedTask;
                            }
                        } ) ) );

        IQuery<TDeclaration> IQuery<TDeclaration>.Where( Func<TDeclaration, bool> predicate ) => this.Where( ( declaration, _ ) => predicate( declaration ) );

        IQuery<TMember> IQuery<TDeclaration>.SelectMany<TMember>( Func<TDeclaration, IEnumerable<TMember>> selector )
            => this.SelectMany( ( declaration, _ ) => selector( declaration ) );

        IQuery<TMember> IQuery<TDeclaration>.Select<TMember>( Func<TDeclaration, TMember> selector )
            => this.Select( ( declaration, _ ) => selector( declaration ) );

        private readonly record struct CachedItem( TDeclaration Declaration, TTag Tag );

        private async Task InvokeAdderAsync(
            QueryExecutionContext queryContext,
            Func<TDeclaration, TTag, QueryExecutionContext, Task> processTarget )
        {
            ConcurrentQueue<CachedItem>? cached = null;
            var processTargetWithCachingIfNecessary = processTarget;

            if ( this.ShouldCache )
            {
                // GetFromCacheAsync uses a semaphore to control exclusivity.  AddToCache must be called is the method returns null.
                cached = await queryContext.GetFromCacheAsync<ConcurrentQueue<CachedItem>>( this, queryContext.CancellationToken );

                if ( cached != null )
                {
                    await this._concurrentTaskRunner.RunConcurrentlyAsync(
                        cached,
                        x => processTarget( x.Declaration, x.Tag, queryContext ),
                        queryContext.CancellationToken );

                    return;
                }
                else
                {
                    cached = new ConcurrentQueue<CachedItem>();

                    processTargetWithCachingIfNecessary = ( a, tag, ctx ) =>
                    {
                        cached.Enqueue( new CachedItem( a, tag ) );

                        return processTarget( a, tag, ctx );
                    };
                }
            }

            var invoker = this.Owner.UserCodeInvoker;

            await invoker.InvokeAsync( () => this._adder( processTargetWithCachingIfNecessary, queryContext ), queryContext.UserCodeExecutionContext );

            if ( this.ShouldCache )
            {
                queryContext.AddToCache( this, cached.AssertNotNull() );
            }
        }

        public async Task InvokeAsync(
            CompilationModel compilation,
            IDiagnosticAdder diagnosticAdder,
            DiagnosticDefinition<(FormattableString Predecessor, IDeclaration Child, IDeclaration Parent)> diagnosticDefinition,
            Func<TDeclaration, object?, QueryExecutionContext, Task> invokeAction,
            CancellationToken cancellationToken )
        {
            var context = new QueryExecutionContext(
                compilation,
                diagnosticAdder,
                this.Owner.UserCodeInvoker,
                this.Owner.UserCodeExecutionContext.WithCompilationAndDiagnosticAdder( compilation, diagnosticAdder ),
                cancellationToken );

            await this.InvokeAdderAsync( context, ProcessTarget );

            Task ProcessTarget( TDeclaration targetDeclaration, TTag tag, QueryExecutionContext queryExecutionContext )
            {
                if ( targetDeclaration == null! )
                {
                    return Task.CompletedTask;
                }

                queryExecutionContext.CancellationToken.ThrowIfCancellationRequested();

                var predecessorInstance = (IAspectPredecessorImpl) this.Owner.AspectPredecessor.Instance;

                // Check that we are targeting a declaration _under_ the current declaration.
                var originatingDeclaration = this.OriginatingDeclaration.GetTarget( compilation ).AssertNotNull();

                var containingDeclaration = originatingDeclaration.DeclarationKind switch
                {
                    DeclarationKind.Compilation => compilation,
                    DeclarationKind.Namespace => originatingDeclaration,
                    _ => originatingDeclaration.GetClosestNamedType()
                         ?? throw new AssertionFailedException( $"Cannot find the containing type of '{originatingDeclaration}'." )
                };

                if ( (!targetDeclaration.IsContainedIn( containingDeclaration ) || targetDeclaration.DeclaringAssembly.IsExternal)
                     && containingDeclaration.DeclarationKind != DeclarationKind.Compilation )
                {
                    context.DiagnosticSink.Report(
                        diagnosticDefinition.CreateRoslynDiagnostic(
                            predecessorInstance.GetDiagnosticLocation( compilation.RoslynCompilation ),
                            (predecessorInstance.FormatPredecessor( compilation ), targetDeclaration, containingDeclaration) ) );

                    return Task.CompletedTask;
                }

                return invokeAction( targetDeclaration, tag, context );
            }
        }
    }
}