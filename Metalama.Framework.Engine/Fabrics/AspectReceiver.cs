// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.CodeFixes;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Source;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.HierarchicalOptions;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Engine.Validation;
using Metalama.Framework.Options;
using Metalama.Framework.Project;
using Metalama.Framework.Validation;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Fabrics
{
    /// <summary>
    /// An implementation of <see cref="IAspectReceiver{TDeclaration}"/>, which offers a fluent
    /// API to programmatically add children aspects.
    /// </summary>
    internal abstract class AspectReceiver<TDeclaration, TTag> : IAspectReceiver<TDeclaration, TTag>
        where TDeclaration : class, IDeclaration
    {
        protected abstract IAspectReceiverParent Parent { get; }

        private readonly CompilationModelVersion _compilationModelVersion;
        private readonly Func<Func<TDeclaration, TTag, DeclarationSelectionContext, Task>, DeclarationSelectionContext, Task> _adder;
        private readonly ILogger _logger;
        private readonly IConcurrentTaskRunner _concurrentTaskRunner;

        // We track the number of children to know if we must cache results.
        private int _childrenCount;

        internal AspectReceiver(
            ProjectServiceProvider serviceProvider,
            IRef<IDeclaration> containingDeclaration,
            CompilationModelVersion compilationModelVersion,
            Func<Func<TDeclaration, TTag, DeclarationSelectionContext, Task>, DeclarationSelectionContext, Task> addTargets )
        {
            this._logger = serviceProvider.GetLoggerFactory().GetLogger( "AspectReceiver" );
            this._concurrentTaskRunner = serviceProvider.GetRequiredService<IConcurrentTaskRunner>();
            this.OriginatingDeclaration = containingDeclaration;
            this._compilationModelVersion = compilationModelVersion;
            this._adder = addTargets;
        }

        public IProject Project => this.Parent.Project;

        public string? OriginatingNamespace => this.Parent.Namespace;

        public IRef<IDeclaration> OriginatingDeclaration { get; }

        protected virtual bool ShouldCache => this._childrenCount > 1;

        private AspectReceiver<TChildDeclaration, TChildTag> AddChild<TChildDeclaration, TChildTag>( AspectReceiver<TChildDeclaration, TChildTag> child )
            where TChildDeclaration : class, IDeclaration
        {
            this._childrenCount++;

            return child;
        }

        private AspectClass? GetAspectClass<TAspect>()
            where TAspect : IAspect
            => this.GetAspectClass( typeof(TAspect) );

        private AspectClass? GetAspectClass( Type aspectType )
        {
            if ( !this.Parent.AspectClasses.Dictionary.TryGetValue( aspectType.FullName.AssertNotNull(), out var aspectClass ) )
            {
                return null;
            }

            if ( aspectClass.IsAbstract )
            {
                throw new ArgumentOutOfRangeException( nameof(aspectType), MetalamaStringFormatter.Format( $"'{aspectType}' is an abstract type." ) );
            }

            return (AspectClass) aspectClass;
        }

        private void RegisterAspectSource( IAspectSource aspectSource )
        {
            this._childrenCount++;

            this.Parent.LicenseVerifier?.VerifyCanAddChildAspect( this.Parent.AspectPredecessor );

            this.Parent.AddAspectSource( aspectSource );
        }

        private void RegisterValidatorSource( ProgrammaticValidatorSource validatorSource )
        {
            this._childrenCount++;

            this.Parent.LicenseVerifier?.VerifyCanAddValidator( this.Parent.AspectPredecessor );

            this.Parent.AddValidatorSource( validatorSource );
        }

        private void RegisterOptionsSource( IHierarchicalOptionsSource hierarchicalOptionsSource )
        {
            this._childrenCount++;

            this.Parent.AddOptionsSource( hierarchicalOptionsSource );
        }

        private Task SelectReferenceValidatorInstancesAsync(
            IDeclaration validatedDeclaration,
            ValidatorDriver driver,
            ValidatorImplementation implementation,
            ReferenceKinds referenceKinds,
            bool includeDerivedTypes,
            ReferenceGranularity granularity,
            OutboundActionCollectionContext context )
        {
            var description = MetalamaStringFormatter.Format(
                $"reference validator for {validatedDeclaration.DeclarationKind} '{validatedDeclaration.ToDisplayString()}' provided by {this.Parent.DiagnosticSourceDescription}" );

            context.Collector.AddValidator(
                new ReferenceValidatorInstance(
                    validatedDeclaration,
                    driver,
                    implementation,
                    referenceKinds,
                    includeDerivedTypes,
                    description,
                    granularity ) );

            if ( validatedDeclaration is SourceMethod validatedMethod )
            {
                switch ( validatedMethod.MethodKind )
                {
                    case MethodKind.PropertyGet:
                        context.Collector.AddValidator(
                            new ReferenceValidatorInstance(
                                validatedMethod.DeclaringMember!,
                                driver,
                                implementation,
                                ReferenceKinds.Default,
                                includeDerivedTypes,
                                description,
                                granularity ) );

                        break;

                    // TODO: The validator doesn't distinguish event add and event remove.
                    case MethodKind.PropertySet:
                    case MethodKind.EventAdd:
                    case MethodKind.EventRemove:
                        context.Collector.AddValidator(
                            new ReferenceValidatorInstance(
                                validatedMethod.DeclaringMember!,
                                driver,
                                implementation,
                                ReferenceKinds.Assignment,
                                includeDerivedTypes,
                                description,
                                granularity ) );

                        break;
                }
            }

            return Task.CompletedTask;
        }

#pragma warning disable CS0612 // Type or member is obsolete

        void IValidatorReceiver.ValidateReferences(
            ValidatorDelegate<ReferenceValidationContext> validateMethod,
            ReferenceKinds referenceKinds,
            bool includeDerivedTypes )
            => this.ValidateInboundReferencesCore( validateMethod, ReferenceGranularity.SyntaxNode, referenceKinds, includeDerivedTypes );

        public void ValidateInboundReferences(
            Action<ReferenceValidationContext> validateMethod,
            ReferenceGranularity granularity,
            ReferenceKinds referenceKinds,
            ReferenceValidationOptions options )
            => this.ValidateInboundReferencesCore( validateMethod, granularity, referenceKinds, options == ReferenceValidationOptions.IncludeDerivedTypes );
#pragma warning restore CS0612 // Type or member is obsolete

        private void ValidateInboundReferencesCore(
            Delegate validateMethod, // Intentionally weakly typed since we accept two signatures.
            ReferenceGranularity granularity,
            ReferenceKinds referenceKinds,
            bool includeDerivedTypes = false )
        {
            var methodInfo = validateMethod.Method;

            if ( methodInfo.DeclaringType?.IsAssignableFrom( this.Parent.Type ) != true )
            {
                throw new ArgumentOutOfRangeException( nameof(validateMethod), $"The delegate must point to a method of type '{this.Parent.Type};." );
            }

            if ( methodInfo.DeclaringType != null &&
                 methodInfo.DeclaringType.GetMethods( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static )
                     .Count( m => m.Name == methodInfo.Name ) > 1 )
            {
                throw new ArgumentOutOfRangeException(
                    nameof(validateMethod),
                    $"The type '{this.Parent.Type}' must have only one method called '{methodInfo.Name}'." );
            }

            var userCodeInvoker = this.Parent.UserCodeInvoker;
            var executionContext = UserCodeExecutionContext.Current;

            this.RegisterValidatorSource(
                new ProgrammaticValidatorSource(
                    this.Parent.GetReferenceValidatorDriver( validateMethod.Method ),
                    ValidatorKind.Reference,
                    CompilationModelVersion.Current,
                    this.Parent.AspectPredecessor,
                    ( source, context ) => this.SelectAndValidateValidatorOrConfiguratorTargetsAsync(
                        userCodeInvoker,
                        executionContext.WithCompilationAndDiagnosticAdder( context.Compilation, context.Collector ),
                        GeneralDiagnosticDescriptors.CanAddValidatorOnlyUnderParent,
                        context,
                        ( item, _, context2 ) => this.SelectReferenceValidatorInstancesAsync(
                            item,
                            source.Driver,
                            ValidatorImplementation.Create( source.Predecessor.Instance ),
                            referenceKinds,
                            includeDerivedTypes,
                            granularity,
                            context2 ) ) ) );
        }

        [Obsolete]
        void IValidatorReceiver.ValidateReferences( ReferenceValidator validator ) => this.ValidateInboundReferences( validator );

        public void ValidateInboundReferences( InboundReferenceValidator validator )
        {
            var userCodeInvoker = this.Parent.UserCodeInvoker;
            var executionContext = UserCodeExecutionContext.Current;

            this.RegisterValidatorSource(
                new ProgrammaticValidatorSource(
                    this.Parent.GetReferenceValidatorDriver( validator.GetType() ),
                    ValidatorKind.Reference,
                    CompilationModelVersion.Current,
                    this.Parent.AspectPredecessor,
                    ( source, context ) => this.SelectAndValidateValidatorOrConfiguratorTargetsAsync(
                        userCodeInvoker,
                        executionContext.WithCompilationAndDiagnosticAdder( context.Compilation, context.Collector ),
                        GeneralDiagnosticDescriptors.CanAddValidatorOnlyUnderParent,
                        context,
                        ( item, _, c ) => this.SelectReferenceValidatorInstancesAsync(
                            item,
                            source.Driver,
                            ValidatorImplementation.Create( validator ),
                            validator.ValidatedReferenceKinds,
                            validator.IncludeDerivedTypes,
                            validator.Granularity,
                            c ) ) ) );
        }

        void IValidatorReceiver<TDeclaration>.ValidateInboundReferences<TValidator>( Func<TDeclaration, TValidator> getValidator )
            => this.ValidateInboundReferences( ( declaration, _ ) => getValidator( declaration ) );

        public void ValidateInboundReferences<TValidator>( Func<TDeclaration, TTag, TValidator> getValidator )
            where TValidator : InboundReferenceValidator
        {
            var userCodeInvoker = this.Parent.UserCodeInvoker;
            var executionContext = UserCodeExecutionContext.Current;

            this.RegisterValidatorSource(
                new ProgrammaticValidatorSource(
                    this.Parent.GetReferenceValidatorDriver( typeof(TValidator) ),
                    ValidatorKind.Reference,
                    CompilationModelVersion.Current,
                    this.Parent.AspectPredecessor,
                    ( source, context ) => this.SelectAndValidateValidatorOrConfiguratorTargetsAsync(
                        userCodeInvoker,
                        executionContext.WithCompilationAndDiagnosticAdder( context.Compilation, context.Collector ),
                        GeneralDiagnosticDescriptors.CanAddValidatorOnlyUnderParent,
                        context,
                        ( item, tag, c ) =>
                        {
                            var validator = userCodeInvoker.Invoke( () => getValidator( item, tag ), executionContext );

                            return this.SelectReferenceValidatorInstancesAsync(
                                item,
                                source.Driver,
                                ValidatorImplementation.Create( validator ),
                                validator.ValidatedReferenceKinds,
                                validator.IncludeDerivedTypes,
                                validator.Granularity,
                                c );
                        } ) ) );
        }

        void IValidatorReceiver<TDeclaration>.SuggestCodeFix( Func<TDeclaration, CodeFix> codeFix )
            => this.SuggestCodeFix( ( declaration, _ ) => codeFix( declaration ) );

        IValidatorReceiver<TDeclaration> IValidatorReceiver<TDeclaration>.AfterAllAspects() => this.AfterAllAspects();

        IValidatorReceiver<TDeclaration> IValidatorReceiver<TDeclaration>.BeforeAnyAspect() => this.BeforeAnyAspect();

        IAspectReceiver<TMember, TTag> IAspectReceiver<TDeclaration, TTag>.SelectMany<TMember>( Func<TDeclaration, IEnumerable<TMember>> selector )
            => this.SelectMany( ( declaration, _ ) => selector( declaration ) );

        IAspectReceiver<TMember, TTag> IAspectReceiver<TDeclaration, TTag>.Select<TMember>( Func<TDeclaration, TMember> selector )
            => this.Select( ( declaration, _ ) => selector( declaration ) );

        IAspectReceiver<INamedType> IAspectReceiver<TDeclaration>.SelectTypes( bool includeNestedTypes ) => this.SelectTypes( includeNestedTypes );

        IAspectReceiver<INamedType, TTag> IAspectReceiver<TDeclaration, TTag>.SelectTypesDerivedFrom( Type baseType, DerivedTypesOptions options )
            => this.SelectTypesDerivedFromCore( c => (INamedType) c.Factory.GetTypeByReflectionType( baseType ), options );

        IAspectReceiver<INamedType, TTag> IAspectReceiver<TDeclaration, TTag>.SelectTypesDerivedFrom(
            INamedType baseType,
            DerivedTypesOptions options )
            => this.SelectTypesDerivedFromCore( _ => baseType, options );

        IAspectReceiver<INamedType> IAspectReceiver<TDeclaration>.SelectTypesDerivedFrom( Type baseType, DerivedTypesOptions options )
            => this.SelectTypesDerivedFromCore( c => (INamedType) c.Factory.GetTypeByReflectionType( baseType ), options );

        IValidatorReceiver<INamedType, TTag> IValidatorReceiver<TDeclaration, TTag>.SelectTypesDerivedFrom( Type baseType, DerivedTypesOptions options )
            => this.SelectTypesDerivedFromCore( c => (INamedType) c.Factory.GetTypeByReflectionType( baseType ), options );

        IValidatorReceiver<INamedType> IValidatorReceiver<TDeclaration>.SelectTypesDerivedFrom( Type baseType, DerivedTypesOptions options )
            => this.SelectTypesDerivedFromCore( c => (INamedType) c.Factory.GetTypeByReflectionType( baseType ), options );

        IAspectReceiver<INamedType> IAspectReceiver<TDeclaration>.SelectTypesDerivedFrom( INamedType baseType, DerivedTypesOptions options )
            => this.SelectTypesDerivedFromCore( _ => baseType, options );

        IValidatorReceiver<INamedType, TTag> IValidatorReceiver<TDeclaration, TTag>.SelectTypesDerivedFrom(
            INamedType baseType,
            DerivedTypesOptions options )
            => this.SelectTypesDerivedFromCore( _ => baseType, options );

        IValidatorReceiver<INamedType> IValidatorReceiver<TDeclaration>.SelectTypesDerivedFrom(
            INamedType baseType,
            DerivedTypesOptions options )
            => this.SelectTypesDerivedFromCore( _ => baseType, options );

        private IAspectReceiver<INamedType, TTag> SelectTypesDerivedFromCore( Func<CompilationModel, INamedType> getBaseType, DerivedTypesOptions options )
            => this.AddChild(
                new ChildAspectReceiver<INamedType, TTag>(
                    this.OriginatingDeclaration,
                    this.Parent,
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

        IAspectReceiver<TDeclaration, TTag> IAspectReceiver<TDeclaration, TTag>.Where( Func<TDeclaration, bool> predicate )
            => this.Where( ( declaration, _ ) => predicate( declaration ) );

        IAspectReceiver<TOut, TTag> IAspectReceiver<TDeclaration, TTag>.OfType<TOut>() => this.OfType<TOut>();

        IAspectReceiver<TOut> IAspectReceiver<TDeclaration>.OfType<TOut>() => this.OfType<TOut>();

        IValidatorReceiver<TOut, TTag> IValidatorReceiver<TDeclaration, TTag>.OfType<TOut>() => this.OfType<TOut>();

        IValidatorReceiver<TOut> IValidatorReceiver<TDeclaration>.OfType<TOut>() => this.OfType<TOut>();

        IAspectReceiver<TDeclaration, TTag1> IAspectReceiver<TDeclaration>.Tag<TTag1>( Func<TDeclaration, TTag1> getTag )
            => this.Tag( ( declaration, _ ) => getTag( declaration ) );

        public IReadOnlyCollection<TDeclaration> ToCollection( ICompilation? compilation )
        {
            var compilationModel = (CompilationModel?) compilation ?? UserCodeExecutionContext.Current.Compilation.AssertNotNull();

            if ( compilationModel.IsPartial )
            {
                throw new InvalidOperationException( "This method cannot be used with a partial compilation (typically at design time." );
            }

            var bag = new ConcurrentQueue<TDeclaration>();

            this.Parent.ServiceProvider.Global.GetRequiredService<ITaskRunner>()
                .RunSynchronously(
                    () => this.InvokeAdderAsync(
                        new DeclarationSelectionContext(
                            compilationModel,
                            CancellationToken.None ),
                        ( declaration, _, _ ) =>
                        {
                            bag.Enqueue( declaration );

                            return Task.CompletedTask;
                        } ) );

            return bag;
        }

        IAspectReceiver<TDeclaration, TNewTag> IAspectReceiver<TDeclaration, TTag>.Tag<TNewTag>( Func<TDeclaration, TNewTag> getTag )
            => this.Tag( ( declaration, _ ) => getTag( declaration ) );

        public IAspectReceiver<TDeclaration, TNewTag> Tag<TNewTag>( Func<TDeclaration, TTag, TNewTag> getTag )
            => this.AddChild(
                new ChildAspectReceiver<TDeclaration, TNewTag>(
                    this.OriginatingDeclaration,
                    this.Parent,
                    this._compilationModelVersion,
                    ( action, context ) => this.InvokeAdderAsync(
                        context,
                        ( declaration, tag, context2 ) =>
                        {
                            var newTag = getTag( declaration, tag );

                            return action( declaration, newTag, context2 );
                        } ) ) );

        IAspectReceiver<TMember> IAspectReceiver<TDeclaration>.SelectMany<TMember>( Func<TDeclaration, IEnumerable<TMember>> selector )
            => this.SelectMany( ( declaration, _ ) => selector( declaration ) );

        IAspectReceiver<TMember> IAspectReceiver<TDeclaration>.Select<TMember>( Func<TDeclaration, TMember> selector )
            => this.Select( ( declaration, _ ) => selector( declaration ) );

        IValidatorReceiver<TMember, TTag> IValidatorReceiver<TDeclaration, TTag>.SelectMany<TMember>( Func<TDeclaration, IEnumerable<TMember>> selector )
            => this.SelectMany( ( declaration, _ ) => selector( declaration ) );

        IValidatorReceiver<TMember, TTag> IValidatorReceiver<TDeclaration, TTag>.Select<TMember>( Func<TDeclaration, TMember> selector )
            => this.Select( ( declaration, _ ) => selector( declaration ) );

        IValidatorReceiver<INamedType, TTag> IValidatorReceiver<TDeclaration, TTag>.SelectTypes( bool includeNestedTypes )
            => this.SelectTypes( includeNestedTypes );

        IAspectReceiver<TDeclaration> IAspectReceiver<TDeclaration>.Where( Func<TDeclaration, bool> predicate )
            => this.Where( ( declaration, _ ) => predicate( declaration ) );

        void IAspectReceiver<TDeclaration>.SetOptions<TOptions>( Func<TDeclaration, TOptions> func )
            => this.SetOptions( ( declaration, _ ) => func( declaration ) );

        IValidatorReceiver<TDeclaration, TTag> IValidatorReceiver<TDeclaration, TTag>.Where( Func<TDeclaration, bool> predicate )
            => this.Where( ( declaration, _ ) => predicate( declaration ) );

        IValidatorReceiver<TDeclaration, TNewTag> IValidatorReceiver<TDeclaration, TTag>.Tag<TNewTag>( Func<TDeclaration, TNewTag> getTag )
            => this.Tag( ( declaration, _ ) => getTag( declaration ) );

        IValidatorReceiver<TDeclaration, TNewTag> IValidatorReceiver<TDeclaration, TTag>.Tag<TNewTag>( Func<TDeclaration, TTag, TNewTag> getTag )
            => this.Tag( getTag );

        IValidatorReceiver<TDeclaration, TTag> IValidatorReceiver<TDeclaration, TTag>.Where( Func<TDeclaration, TTag, bool> predicate )
            => this.Where( predicate );

        IValidatorReceiver<TMember, TTag> IValidatorReceiver<TDeclaration, TTag>.Select<TMember>( Func<TDeclaration, TTag, TMember> selector )
            => this.Select( selector );

        IValidatorReceiver<TMember, TTag> IValidatorReceiver<TDeclaration, TTag>.SelectMany<TMember>( Func<TDeclaration, TTag, IEnumerable<TMember>> selector )
            => this.SelectMany( selector );

        public void Validate( ValidatorDelegate<DeclarationValidationContext> validateMethod )
        {
            var userCodeInvoker = this.Parent.UserCodeInvoker;
            var executionContext = UserCodeExecutionContext.Current;

            this.RegisterValidatorSource(
                new ProgrammaticValidatorSource(
                    this.Parent,
                    ValidatorKind.Definition,
                    this._compilationModelVersion,
                    this.Parent.AspectPredecessor,
                    validateMethod,
                    ( source, context ) => this.SelectAndValidateValidatorOrConfiguratorTargetsAsync(
                        userCodeInvoker,
                        executionContext.WithCompilationAndDiagnosticAdder( context.Compilation, context.Collector ),
                        GeneralDiagnosticDescriptors.CanAddValidatorOnlyUnderParent,
                        context,
                        ( item, tag, context2 ) =>
                        {
                            context2.Collector.AddValidator(
                                new DeclarationValidatorInstance(
                                    item,
                                    (ValidatorDriver<DeclarationValidationContext>) source.Driver,
                                    ValidatorImplementation.Create( source.Predecessor.Instance ),
                                    this.Parent.DiagnosticSourceDescription,
                                    tag ) );

                            return Task.CompletedTask;
                        } ) ) );
        }

        void IValidatorReceiver<TDeclaration>.ValidateReferences<TValidator>( Func<TDeclaration, TValidator> validator )
            => this.ValidateInboundReferences( ( declaration, _ ) => validator( declaration ) );

        void IValidatorReceiver<TDeclaration>.ReportDiagnostic( Func<TDeclaration, IDiagnostic> diagnostic )
            => this.ReportDiagnostic( ( declaration, _ ) => diagnostic( declaration ) );

        void IValidatorReceiver<TDeclaration>.SuppressDiagnostic( Func<TDeclaration, SuppressionDefinition> suppression )
            => this.SuppressDiagnostic( ( declaration, _ ) => suppression( declaration ) );

        public void ReportDiagnostic( Func<TDeclaration, TTag, IDiagnostic> diagnostic )
            => this.Validate( new FinalValidatorHelper<IDiagnostic>( diagnostic ).ReportDiagnostic );

        public void SuppressDiagnostic( Func<TDeclaration, TTag, SuppressionDefinition> suppression )
            => this.Validate( new FinalValidatorHelper<SuppressionDefinition>( suppression ).SuppressDiagnostic );

        public void SuggestCodeFix( Func<TDeclaration, TTag, CodeFix> codeFix ) => this.Validate( new FinalValidatorHelper<CodeFix>( codeFix ).SuggestCodeFix );

        public IValidatorReceiver<TDeclaration, TTag> AfterAllAspects()
            => this.AddChild(
                new ChildAspectReceiver<TDeclaration, TTag>( this.OriginatingDeclaration, this.Parent, CompilationModelVersion.Final, this._adder ) );

        public IValidatorReceiver<TDeclaration, TTag> BeforeAnyAspect()
            => this.AddChild(
                new ChildAspectReceiver<TDeclaration, TTag>( this.OriginatingDeclaration, this.Parent, CompilationModelVersion.Initial, this._adder ) );

        public IAspectReceiver<TMember, TTag> SelectMany<TMember>( Func<TDeclaration, TTag, IEnumerable<TMember>> selector )
            where TMember : class, IDeclaration
            => this.AddChild(
                new ChildAspectReceiver<TMember, TTag>(
                    this.OriginatingDeclaration,
                    this.Parent,
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

        public IAspectReceiver<TMember, TTag> Select<TMember>( Func<TDeclaration, TTag, TMember> selector )
            where TMember : class, IDeclaration
            => this.AddChild(
                new ChildAspectReceiver<TMember, TTag>(
                    this.OriginatingDeclaration,
                    this.Parent,
                    this._compilationModelVersion,
                    ( action, context ) => this.InvokeAdderAsync(
                        context,
                        ( declaration, tag, context2 ) => action( selector( declaration, tag ), tag, context2 ) ) ) );

        public IAspectReceiver<INamedType, TTag> SelectTypes( bool includeNestedTypes = true )
            => this.AddChild(
                new ChildAspectReceiver<INamedType, TTag>(
                    this.OriginatingDeclaration,
                    this.Parent,
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

        IValidatorReceiver<INamedType> IValidatorReceiver<TDeclaration>.SelectTypes( bool includeNestedTypes ) => this.SelectTypes( includeNestedTypes );

        public IAspectReceiver<TDeclaration, TTag> Where( Func<TDeclaration, TTag, bool> predicate )
            => this.AddChild(
                new ChildAspectReceiver<TDeclaration, TTag>(
                    this.OriginatingDeclaration,
                    this.Parent,
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

        private IAspectReceiver<TOut, TTag> OfType<TOut>()
            where TOut : class, IDeclaration
            => this.AddChild(
                new ChildAspectReceiver<TOut, TTag>(
                    this.OriginatingDeclaration,
                    this.Parent,
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

        public void SetOptions<TOptions>( Func<TDeclaration, TTag, TOptions> func )
            where TOptions : class, IHierarchicalOptions, IHierarchicalOptions<TDeclaration>, new()
        {
            var userCodeInvoker = this.Parent.UserCodeInvoker;
            var executionContext = UserCodeExecutionContext.Current;

            this.RegisterOptionsSource(
                new ProgrammaticHierarchicalOptionsSource(
                    ( context )
                        => this.SelectAndValidateValidatorOrConfiguratorTargetsAsync(
                            userCodeInvoker,
                            executionContext.WithCompilationAndDiagnosticAdder( context.Compilation, context.Collector ),
                            GeneralDiagnosticDescriptors.CanAddValidatorOnlyUnderParent,
                            context,
                            ( declaration, tag, context2 ) =>
                            {
                                if ( userCodeInvoker.TryInvoke(
                                        () => func( declaration, tag ),
                                        executionContext,
                                        out var options ) )
                                {
                                    context2.Collector.AddOptions(
                                        new HierarchicalOptionsInstance(
                                            declaration,
                                            options ) );
                                }

                                return Task.CompletedTask;
                            } ) ) );
        }

        public void SetOptions<TOptions>( TOptions options )
            where TOptions : class, IHierarchicalOptions, IHierarchicalOptions<TDeclaration>, new()
        {
            var userCodeInvoker = this.Parent.UserCodeInvoker;
            var executionContext = UserCodeExecutionContext.Current;

            this.RegisterOptionsSource(
                new ProgrammaticHierarchicalOptionsSource(
                    ( context )
                        => this.SelectAndValidateValidatorOrConfiguratorTargetsAsync(
                            userCodeInvoker,
                            executionContext.WithCompilationAndDiagnosticAdder( context.Compilation, context.Collector ),
                            GeneralDiagnosticDescriptors.CanAddValidatorOnlyUnderParent,
                            context,
                            ( declaration, _, context2 ) =>
                            {
                                context2.Collector.AddOptions(
                                    new HierarchicalOptionsInstance(
                                        declaration,
                                        options ) );

                                return Task.CompletedTask;
                            } ) ) );
        }

        IValidatorReceiver<TDeclaration> IValidatorReceiver<TDeclaration>.Where( Func<TDeclaration, bool> predicate )
            => this.Where( ( declaration, _ ) => predicate( declaration ) );

        IValidatorReceiver<TDeclaration, TNewTag> IValidatorReceiver<TDeclaration>.Tag<TNewTag>( Func<TDeclaration, TNewTag> getTag )
            => this.Tag( ( declaration, _ ) => getTag( declaration ) );

        IValidatorReceiver<TMember> IValidatorReceiver<TDeclaration>.SelectMany<TMember>( Func<TDeclaration, IEnumerable<TMember>> selector )
            => this.SelectMany( ( declaration, _ ) => selector( declaration ) );

        IValidatorReceiver<TMember> IValidatorReceiver<TDeclaration>.Select<TMember>( Func<TDeclaration, TMember> selector )
            => this.Select( ( declaration, _ ) => selector( declaration ) );

        private sealed class FinalValidatorHelper<TOutput>
        {
            private readonly Func<TDeclaration, TTag, TOutput> _func;

            public FinalValidatorHelper( Func<TDeclaration, TTag, TOutput> func )
            {
                this._func = func;
            }

            public void ReportDiagnostic( in DeclarationValidationContext context )
                => context.Diagnostics.Report( (IDiagnostic) this._func( (TDeclaration) context.Declaration, (TTag) context.Tag! )! );

            public void SuppressDiagnostic( in DeclarationValidationContext context )
                => context.Diagnostics.Suppress( (SuppressionDefinition) (object) this._func( (TDeclaration) context.Declaration, (TTag) context.Tag! )! );

            public void SuggestCodeFix( in DeclarationValidationContext context )
                => context.Diagnostics.Suggest( (CodeFix) (object) this._func( (TDeclaration) context.Declaration, (TTag) context.Tag! )! );
        }

        public void AddAspect<TAspect>( Func<TDeclaration, TTag, TAspect> createAspect )
            where TAspect : class, IAspect<TDeclaration>
            => this.AddAspectIfEligible( createAspect, EligibleScenarios.None );

        public void AddAspect( Type aspectType, Func<TDeclaration, TTag, IAspect> createAspect )
            => this.AddAspectIfEligible( aspectType, createAspect, EligibleScenarios.None );

        public void AddAspectIfEligible( Type aspectType, Func<TDeclaration, TTag, IAspect> createAspect, EligibleScenarios eligibility )
        {
            var aspectClass = this.GetAspectClass( aspectType );

            if ( aspectClass == null )
            {
                // The aspect class was not found. We're going to assume that this happened because of an already-reported error and do nothing.

                this._logger.Warning?.Log( $"The aspect type {aspectType} was not found when calling AddAspectIfEligible." );

                return;
            }

            var userCodeInvoker = this.Parent.UserCodeInvoker;
            var executionContext = UserCodeExecutionContext.Current;

            this.RegisterAspectSource(
                new ProgrammaticAspectSource(
                    aspectClass,
                    ( context ) => this.SelectAndValidateAspectTargetsAsync(
                        userCodeInvoker,
                        executionContext.WithCompilationAndDiagnosticAdder( context.Compilation, context.Collector ),
                        aspectClass,
                        eligibility,
                        context,
                        ( item, tag, context2 ) =>
                        {
                            if ( userCodeInvoker.TryInvoke(
                                    () => createAspect( item, tag ),
                                    executionContext,
                                    out var aspect ) )
                            {
                                context2.Collector.AddAspectInstance(
                                    new AspectInstance(
                                        aspect,
                                        item,
                                        aspectClass,
                                        this.Parent.AspectPredecessor ) );
                            }

                            return Task.CompletedTask;
                        } ) ) );
        }

        public void AddAspectIfEligible<TAspect>( Func<TDeclaration, TTag, TAspect> createAspect, EligibleScenarios eligibility )
            where TAspect : class, IAspect<TDeclaration>
            => this.AddAspectIfEligible( typeof(TAspect), createAspect, eligibility );

        void IAspectReceiver<TDeclaration>.AddAspect( Type aspectType, Func<TDeclaration, IAspect> createAspect )
            => this.AddAspect( aspectType, ( declaration, _ ) => createAspect( declaration ) );

        void IAspectReceiver<TDeclaration>.AddAspectIfEligible(
            Type aspectType,
            Func<TDeclaration, IAspect> createAspect,
            EligibleScenarios eligibility )
            => this.AddAspectIfEligible( aspectType, ( declaration, _ ) => createAspect( declaration ), eligibility );

        void IAspectReceiver<TDeclaration>.AddAspect<TAspect>( Func<TDeclaration, TAspect> createAspect )
            => this.AddAspect( ( declaration, _ ) => createAspect( declaration ) );

        void IAspectReceiver<TDeclaration>.AddAspectIfEligible<TAspect>(
            Func<TDeclaration, TAspect> createAspect,
            EligibleScenarios eligibility )
            => this.AddAspectIfEligible( ( declaration, _ ) => createAspect( declaration ), eligibility );

        public void AddAspect<TAspect>()
            where TAspect : class, IAspect<TDeclaration>, new()
            => this.AddAspectIfEligible<TAspect>( EligibleScenarios.None );

        public void AddAspectIfEligible<TAspect>( EligibleScenarios eligibility )
            where TAspect : class, IAspect<TDeclaration>, new()
        {
            var aspectClass = this.GetAspectClass<TAspect>();

            if ( aspectClass == null )
            {
                // The aspect class was not found. We're going to assume that this happened because of an already-reported error and do nothing.

                this._logger.Warning?.Log( $"The aspect type {typeof(TAspect)} was not found when calling AddAspectIfEligible." );

                return;
            }

            var userCodeInvoker = this.Parent.UserCodeInvoker;
            var executionContext = UserCodeExecutionContext.Current;

            this.RegisterAspectSource(
                new ProgrammaticAspectSource(
                    aspectClass,
                    ( context ) => this.SelectAndValidateAspectTargetsAsync(
                        userCodeInvoker,
                        executionContext.WithCompilationAndDiagnosticAdder( context.Compilation, context.Collector ),
                        aspectClass,
                        eligibility,
                        context,
                        ( t, _, context2 ) =>
                        {
                            if ( userCodeInvoker.TryInvoke(
                                    () => new TAspect(),
                                    executionContext,
                                    out var aspect ) )
                            {
                                context2.Collector.AddAspectInstance(
                                    new AspectInstance(
                                        aspect,
                                        t,
                                        aspectClass,
                                        this.Parent.AspectPredecessor ) );
                            }

                            return Task.CompletedTask;
                        } ) ) );
        }

        private readonly record struct CachedItem( TDeclaration Declaration, TTag Tag );

        private async Task InvokeAdderAsync(
            DeclarationSelectionContext selectionContext,
            Func<TDeclaration, TTag, DeclarationSelectionContext, Task> processTarget,
            UserCodeInvoker? invoker = null,
            UserCodeExecutionContext? executionContext = null )
        {
            ConcurrentQueue<CachedItem>? cached = null;
            var processTargetWithCachingIfNecessary = processTarget;

            if ( this.ShouldCache )
            {
                // GetFromCacheAsync uses a semaphore to control exclusivity.  AddToCache must be called is the method returns null.
                cached = await selectionContext.GetFromCacheAsync<ConcurrentQueue<CachedItem>>( this, selectionContext.CancellationToken );

                if ( cached != null )
                {
                    await this._concurrentTaskRunner.RunConcurrentlyAsync(
                        cached,
                        x => processTarget( x.Declaration, x.Tag, selectionContext ),
                        selectionContext.CancellationToken );

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

            if ( invoker != null && executionContext != null )
            {
#if DEBUG
                if ( !ReferenceEquals( executionContext.Compilation, selectionContext.Compilation ) )
                {
                    throw new AssertionFailedException( "Execution context mismatch." );
                }
#endif

                await invoker.InvokeAsync( () => this._adder( processTargetWithCachingIfNecessary, selectionContext ), executionContext );
            }
            else
            {
                await this._adder( processTargetWithCachingIfNecessary, selectionContext );
            }

            if ( this.ShouldCache )
            {
                selectionContext.AddToCache( this, cached.AssertNotNull() );
            }
        }

        private async Task SelectAndValidateAspectTargetsAsync(
            UserCodeInvoker? invoker,
            UserCodeExecutionContext? executionContext,
            AspectClass aspectClass,
            EligibleScenarios filteredEligibility,
            OutboundActionCollectionContext context,
            Func<TDeclaration, TTag, OutboundActionCollectionContext, Task> addResult )
        {
            var compilation = context.Compilation;

            await this.InvokeAdderAsync( context, ProcessTarget, invoker, executionContext );

            Task ProcessTarget( TDeclaration targetDeclaration, TTag tag, DeclarationSelectionContext context2 )
            {
                context2.CancellationToken.ThrowIfCancellationRequested();

                if ( targetDeclaration == null! )
                {
                    return Task.CompletedTask;
                }

                var predecessorInstance = (IAspectPredecessorImpl) this.Parent.AspectPredecessor.Instance;

                // Verify containment.
                var containingDeclaration = this.OriginatingDeclaration.GetTarget( compilation ).AssertNotNull();

                if ( !(targetDeclaration.IsContainedIn( containingDeclaration )
                       || (containingDeclaration is IParameter p && p.DeclaringMember.Equals( targetDeclaration ))
                       || (containingDeclaration is IMember m && m.DeclaringType.Equals( targetDeclaration )))
                     || targetDeclaration.DeclaringAssembly.IsExternal )
                {
                    context.Collector.Report(
                        GeneralDiagnosticDescriptors.CanAddChildAspectOnlyUnderParent.CreateRoslynDiagnostic(
                            predecessorInstance.GetDiagnosticLocation( compilation.RoslynCompilation ),
                            (predecessorInstance.FormatPredecessor( compilation ), aspectClass.ShortName, targetDeclaration, containingDeclaration) ) );

                    return Task.CompletedTask;
                }

                // Verify eligibility.
                var eligibility = aspectClass.GetEligibility( targetDeclaration );

                if ( filteredEligibility != EligibleScenarios.None && !eligibility.IncludesAny( filteredEligibility ) )
                {
                    return Task.CompletedTask;
                }

                var canBeInherited = ((IDeclarationImpl) targetDeclaration).CanBeInherited;
                var requiredEligibility = canBeInherited ? EligibleScenarios.Default | EligibleScenarios.Inheritance : EligibleScenarios.Default;

                if ( !eligibility.IncludesAny( requiredEligibility ) )
                {
                    var reason = aspectClass.GetIneligibilityJustification( requiredEligibility, new DescribedObject<IDeclaration>( targetDeclaration ) )!;

                    context.Collector.Report(
                        GeneralDiagnosticDescriptors.IneligibleChildAspect.CreateRoslynDiagnostic(
                            predecessorInstance.GetDiagnosticLocation( compilation.RoslynCompilation ),
                            (predecessorInstance.FormatPredecessor( compilation ), aspectClass.ShortName, targetDeclaration, reason) ) );

                    return Task.CompletedTask;
                }

                if ( invoker != null && executionContext != null )
                {
                    return invoker.InvokeAsync( () => addResult( targetDeclaration, tag, context ), executionContext );
                }
                else
                {
                    return addResult( targetDeclaration, tag, context );
                }
            }
        }

        private async Task SelectAndValidateValidatorOrConfiguratorTargetsAsync(
            UserCodeInvoker? invoker,
            UserCodeExecutionContext? executionContext,
            DiagnosticDefinition<(FormattableString Predecessor, IDeclaration Child, IDeclaration Parent)> diagnosticDefinition,
            OutboundActionCollectionContext context,
            Func<TDeclaration, TTag, OutboundActionCollectionContext, Task> addAction )
        {
            var compilation = context.Compilation;

            await this.InvokeAdderAsync( context, ProcessTarget, invoker, executionContext );

            Task ProcessTarget( TDeclaration targetDeclaration, TTag tag, DeclarationSelectionContext context2 )
            {
                if ( targetDeclaration == null! )
                {
                    return Task.CompletedTask;
                }

                context2.CancellationToken.ThrowIfCancellationRequested();

                var predecessorInstance = (IAspectPredecessorImpl) this.Parent.AspectPredecessor.Instance;

                var containingTypeOrCompilation = (IDeclaration?) this.OriginatingDeclaration.GetTarget( compilation ).AssertNotNull().GetTopmostNamedType()
                                                  ?? compilation;

                if ( (!targetDeclaration.IsContainedIn( containingTypeOrCompilation ) || targetDeclaration.DeclaringAssembly.IsExternal)
                     && containingTypeOrCompilation.DeclarationKind != DeclarationKind.Compilation )
                {
                    context.Collector.Report(
                        diagnosticDefinition.CreateRoslynDiagnostic(
                            predecessorInstance.GetDiagnosticLocation( compilation.RoslynCompilation ),
                            (predecessorInstance.FormatPredecessor( compilation ), targetDeclaration, containingTypeOrCompilation) ) );
                }

                return addAction( targetDeclaration, tag, context );
            }
        }

        public void RequireAspect<TAspect>()
            where TAspect : class, IAspect<TDeclaration>, new()
        {
            var aspectClass = this.GetAspectClass<TAspect>();

            if ( aspectClass == null )
            {
                // The aspect class was not found. We're going to assume that this happened because of an already-reported error and do nothing.

                this._logger.Warning?.Log( $"The aspect type {typeof(TAspect)} was not found when calling RequireAspect." );

                return;
            }

            var userCodeInvoker = this.Parent.UserCodeInvoker;
            var executionContext = UserCodeExecutionContext.Current;

            this.RegisterAspectSource(
                new ProgrammaticAspectSource(
                    aspectClass,
                    ( context ) => this.SelectAndValidateAspectTargetsAsync(
                        userCodeInvoker,
                        executionContext.WithCompilationAndDiagnosticAdder( context.Compilation, context.Collector ),
                        aspectClass,
                        EligibleScenarios.None,
                        context,
                        ( declaration, _, context2 ) =>
                        {
                            context2.Collector.AddAspectRequirement(
                                new AspectRequirement(
                                    declaration.ToRef(),
                                    this.Parent.AspectPredecessor.Instance ) );

                            return Task.CompletedTask;
                        } ) ) );
        }
    }
}