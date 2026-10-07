// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility.Synthesis;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Services;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.Extensibility.CallSites
{
    public sealed partial class ExtensionTransformationFactory
    {
        /// <summary>
        /// The transformations that introduce the declared types and methods and generate the bodies of the methods, in the order of the requests.
        /// Protected by <see cref="_sync"/>.
        /// </summary>
        private readonly List<ITransformation> _synthesizedTransformations = [];

        /// <summary>
        /// The names that the factory gave to the declared members, keyed by the full name of the containing type, and the names of the declared
        /// types, keyed by the full name of the containing namespace. Protected by <see cref="_sync"/>.
        /// </summary>
        private readonly Dictionary<string, HashSet<string>> _reservedNames = new( StringComparer.Ordinal );

        /// <summary>
        /// The aspect instances that represent project and namespace fabrics in the transformations, keyed by aspect layer. Protected by
        /// <see cref="_sync"/>.
        /// </summary>
        private readonly Dictionary<AspectLayerId, AspectInstance> _fabricAspectInstances = new();

        /// <summary>
        /// The namespaces that the factory declared for the declared static classes, keyed by full name. Protected by <see cref="_sync"/>.
        /// </summary>
        private readonly Dictionary<string, NamespaceBuilder> _declaredNamespaces = new( StringComparer.Ordinal );

        /// <summary>
        /// Declares a static class, in which <see cref="DeclareMethod"/> can declare methods.
        /// </summary>
        /// <param name="origin">The aspect or fabric that requested the class.</param>
        /// <param name="request">The request.</param>
        /// <returns>The handle of the class.</returns>
        /// <remarks>
        /// The class is not part of the code model that aspects observe. It is emitted in a new syntax tree.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="origin"/> or <paramref name="request"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The request is not valid.</exception>
        /// <exception cref="InvalidOperationException">The factory was completed.</exception>
        public SynthesizedTypeHandle DeclareStaticClass( ExtensionContributionOrigin origin, SynthesizedStaticClassRequest request )
        {
            _ = request ?? throw new ArgumentNullException( nameof(request) );

            var layerInstance = this.GetAspectLayerInstance( origin );

            NamedTypeBuilder builder;

            lock ( this._sync )
            {
                this.ThrowIfCompleted();

                var ns = this.GetOrDeclareNamespace( layerInstance, request.Namespace );
                var isDeclaredNamespace = ns is NamespaceBuilder;

                var name = this.ReserveName(
                    "N:" + ns.FullName,
                    request.NameHint,
                    candidate => isDeclaredNamespace || (!ns.Types.OfName( candidate ).Any() && ns.Namespaces.OfName( candidate ) == null),
                    null );

                builder = new NamedTypeBuilder( layerInstance, ns, name, TypeKind.Class ) { Accessibility = request.Accessibility, IsStatic = true };
                builder.Freeze();

                var transformation = builder.CreateTransformation();
                this.AddSynthesizedTransformation( transformation );
            }

            return new SynthesizedTypeHandle( builder, builder.BuilderData.ToRef().GetTarget( this._compilation ) );
        }

        /// <summary>
        /// Declares a method whose body is generated from a template.
        /// </summary>
        /// <param name="origin">The aspect or fabric that requested the method.</param>
        /// <param name="request">The request.</param>
        /// <returns>The handle of the method, which can be passed to <see cref="CallSiteRedirectionTarget.Synthesized"/>.</returns>
        /// <remarks>
        /// <para>
        /// The method is not part of the code model that aspects observe. The factory gives it a unique name in its type, binds the template, and
        /// creates the transformations that the linker applies. The template is expanded by the linker, which reports the diagnostics of the
        /// template at <see cref="SynthesizedMethodRequest.DiagnosticLocation"/>. When the expansion fails, the call sites redirected to the
        /// method are left unchanged.
        /// </para>
        /// <para>
        /// The numeric suffixes of the names depend on the order of the calls. A caller that requires a deterministic output must call this method
        /// in a deterministic order.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="origin"/> or <paramref name="request"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The request is not valid, for instance because the template does not exist or cannot implement the
        /// method.</exception>
        /// <exception cref="InvalidOperationException">The factory was completed.</exception>
        public SynthesizedMethodHandle DeclareMethod( ExtensionContributionOrigin origin, SynthesizedMethodRequest request )
        {
            _ = request ?? throw new ArgumentNullException( nameof(request) );

            var layerInstance = this.GetAspectLayerInstance( origin );

            var declaringType = request.Placement.SynthesizedType?.Type
                                ?? request.Placement.Type!.ForCompilation( this._compilation )
                                ?? throw new ArgumentException( "The type of the placement does not belong to the compilation.", nameof(request) );

            if ( declaringType.DeclaringAssembly.IsExternal
                 || declaringType.TypeKind is not (TypeKind.Class or TypeKind.Struct) )
            {
                throw new ArgumentException(
                    $"The method cannot be declared in '{declaringType}', because only a class, a struct or a record of the current compilation can contain it.",
                    nameof(request) );
            }

            var metaExtensions = request.Template.MetaExtensions.IsDefault ? ImmutableArray<IMetaExtension>.Empty : request.Template.MetaExtensions;

            if ( metaExtensions.Select( e => e.GetType() ).Distinct().Count() != metaExtensions.Length )
            {
                throw new ArgumentException( "Two meta extensions of the template have the same type.", nameof(request) );
            }

            string name;

            lock ( this._sync )
            {
                this.ThrowIfCompleted();

                name = this.ReserveName(
                    "T:" + declaringType.FullName + "`" + declaringType.TypeParameters.Count,
                    request.NameHint,
                    candidate => IsMemberNameAvailable( declaringType, candidate ),
                    request.IsNameAvailable );
            }

            var serviceProvider = this._serviceProvider;
            var templateProvider = request.Template.TemplateProvider.IsNull ? origin.DefaultTemplateProvider : request.Template.TemplateProvider;

            if ( !serviceProvider.GetRequiredService<TemplateClassProvider>().TryGet( templateProvider, out var templateClass ) )
            {
                throw new ArgumentException( $"The template provider '{templateProvider}' is not a known template provider.", nameof(request) );
            }

            var objectReaderFactory = serviceProvider.GetRequiredService<IObjectReaderFactory>();

            MethodBuilder builder;
            BoundTemplateMethod boundTemplate;
            ProceedBinding proceedBinding;

            using ( UserCodeExecutionContext.WithContext( serviceProvider, this._compilation, $"declaration of the method '{name}'" ) )
            {
                try
                {
                    builder = new MethodBuilder( layerInstance, declaringType, name ) { Accessibility = Accessibility.Private };

                    request.BuildSignature( builder );

                    if ( builder.Name != name )
                    {
                        throw new ArgumentException( "The signature builder must not change the name of the method.", nameof(request) );
                    }

                    var templateMember = MethodTemplateSelection.Select( templateClass, builder, request.Template.Selector )
                        .GetTemplateMember<IMethod>( this._compilation, serviceProvider, templateProvider, objectReaderFactory.GetReader( request.Template.Tags ) );

                    builder.IsAsync = templateMember.GetDeclaration( this._compilation ).IsAsync;
                    builder.SetIsIteratorMethod( templateMember.IsIteratorMethod );
                    builder.Freeze();

                    proceedBinding = request.CreateProceedBinding( builder ) ?? throw new ArgumentException( "The proceed binding is null.", nameof(request) );

                    boundTemplate = templateMember.ForSynthesizedMethod(
                        builder,
                        request.Template.HiddenLeadingParameterCount,
                        request.Template.NameOnlyTrailingParameterCount,
                        objectReaderFactory.GetReader( request.Template.Arguments ) );
                }
                catch ( InvalidTemplateSignatureException e )
                {
                    throw new ArgumentException( e.Message, nameof(request), e );
                }
                catch ( DiagnosticException e )
                {
                    throw new ArgumentException( e.Message, nameof(request), e );
                }
            }

            var introduction = builder.ToTransformation();

            var body = new SynthesizedMethodBodyTransformation(
                layerInstance,
                builder.ToFullRef(),
                boundTemplate,
                proceedBinding,
                request.DiagnosticLocation,
                metaExtensions,
                origin.AspectInstance );

            lock ( this._sync )
            {
                this.ThrowIfCompleted();
                this.AddSynthesizedTransformation( introduction );
                this.AddSynthesizedTransformation( body );
            }

            return new SynthesizedMethodHandle( builder, body );
        }

        /// <summary>
        /// Returns the namespace of a given full name, and declares the parts of the name that do not exist in the compilation. The caller must hold
        /// <see cref="_sync"/>.
        /// </summary>
        private INamespace GetOrDeclareNamespace( AspectLayerInstance layerInstance, string? fullName )
        {
            var ns = this._compilation.GlobalNamespace;

            if ( string.IsNullOrEmpty( fullName ) )
            {
                return ns;
            }

            var prefix = "";

            foreach ( var part in fullName!.Split( '.' ) )
            {
                SynthesisNames.ValidateIdentifier( part, "request" );
                prefix = prefix.Length == 0 ? part : prefix + "." + part;

                if ( ns is not NamespaceBuilder && ns.Namespaces.OfName( part ) is { } existingNamespace )
                {
                    ns = existingNamespace;

                    continue;
                }

                if ( !this._declaredNamespaces.TryGetValue( prefix, out var namespaceBuilder ) )
                {
                    namespaceBuilder = new NamespaceBuilder( layerInstance, ns, part );
                    namespaceBuilder.Freeze();
                    this.AddSynthesizedTransformation( namespaceBuilder.CreateTransformation() );
                    this._declaredNamespaces.Add( prefix, namespaceBuilder );
                }

                ns = namespaceBuilder;
            }

            return ns;
        }

        /// <summary>
        /// Adds a transformation, ordered after all the transformations of the aspects. The caller must hold <see cref="_sync"/>.
        /// </summary>
        private void AddSynthesizedTransformation( ITransformation transformation )
        {
            transformation.SetAdviceOrderingIndices( new AdviceOrderingIndices( int.MaxValue, 0, this._synthesizedTransformations.Count ) );
            this._synthesizedTransformations.Add( transformation );
        }

        /// <summary>
        /// Returns the first name, among the hint and the hint followed by a number, that the factory has not reserved in the scope and that the
        /// delegates accept, and reserves it. The caller must hold <see cref="_sync"/>.
        /// </summary>
        private string ReserveName( string scopeKey, string hint, Func<string, bool> isAvailable, Func<string, bool>? isAvailableForCaller )
        {
            if ( !this._reservedNames.TryGetValue( scopeKey, out var reserved ) )
            {
                reserved = new HashSet<string>( StringComparer.Ordinal );
                this._reservedNames.Add( scopeKey, reserved );
            }

            for ( var i = 0;; i++ )
            {
                var candidate = i == 0 ? hint : hint + i;

                if ( !reserved.Contains( candidate ) && isAvailable( candidate ) && isAvailableForCaller?.Invoke( candidate ) != false )
                {
                    reserved.Add( candidate );

                    return candidate;
                }
            }
        }

        /// <summary>
        /// Determines whether a type can declare a member of a given name: the name must differ from the name of the type and of its type
        /// parameters, and from the names of the members and nested types of the type and of its base types.
        /// </summary>
        private static bool IsMemberNameAvailable( INamedType type, string name )
        {
            if ( type.Name == name || type.TypeParameters.Any( p => p.Name == name ) )
            {
                return false;
            }

            for ( var t = type; t != null; t = t.BaseType )
            {
                if ( t.Members().Any( m => m.Name == name ) || t.Types.OfName( name ).Any() )
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns the aspect layer instance to which the transformations requested by an origin are attributed.
        /// </summary>
        private AspectLayerInstance GetAspectLayerInstance( ExtensionContributionOrigin origin )
        {
            var layer = this.GetOrderedLayer( origin );

            if ( origin.AspectInstance != null )
            {
                return new AspectLayerInstance( origin.AspectInstance, layer.LayerName, this._compilation );
            }

            // A project or namespace fabric has no aspect instance. The transformations are attributed to an instance of the top-level fabric
            // aspect class, whose predecessor is the fabric.
            var aspectClass = layer.AspectClassIfAny
                              ?? throw new ArgumentException( $"The aspect layer '{origin.AspectLayerId}' of the origin has no aspect class.", nameof(origin) );

            lock ( this._sync )
            {
                if ( !this._fabricAspectInstances.TryGetValue( layer.AspectLayerId, out var aspectInstance ) )
                {
                    aspectInstance = new AspectInstance(
                        FabricOriginAspect.Instance,
                        this._compilation.ToRef(),
                        0,
                        aspectClass,
                        [],
                        [origin.Predecessor],
                        false );

                    this._fabricAspectInstances.Add( layer.AspectLayerId, aspectInstance );
                }

                return new AspectLayerInstance( aspectInstance, layer.LayerName, this._compilation );
            }
        }

        /// <summary>
        /// Returns the ordered aspect layer of an origin.
        /// </summary>
        private OrderedAspectLayer GetOrderedLayer( ExtensionContributionOrigin origin )
        {
            _ = origin ?? throw new ArgumentNullException( nameof(origin) );

            // A project or namespace fabric is processed by the top-level fabric aspect class, whose layer is identified by the type of Fabric.
            return this._aspectLayers.FirstOrDefault( l => l.AspectLayerId == origin.AspectLayerId )
                   ?? (origin.Predecessor.Kind == AspectPredecessorKind.Fabric
                       ? this._aspectLayers.FirstOrDefault( l => l.AspectName == typeof(Fabric).FullName )
                       : null)
                   ?? throw new ArgumentException(
                       $"The aspect layer '{origin.AspectLayerId}' of the origin is not an ordered layer of the pipeline.",
                       nameof(origin) );
        }

        /// <summary>
        /// Returns the transformations of the declared types and methods. The caller must hold <see cref="_sync"/>.
        /// </summary>
        private ImmutableArray<ITransformation> GetSynthesizedTransformations() => this._synthesizedTransformations.ToImmutableArray();

        /// <summary>
        /// The aspect object of the aspect instance that represents a project or namespace fabric in the transformations. It is never executed.
        /// </summary>
        private sealed class FabricOriginAspect : IAspect
        {
            public static FabricOriginAspect Instance { get; } = new();
        }
    }
}
