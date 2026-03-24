// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if ROSLYN_5_0_0_OR_GREATER
using Metalama.Framework.Engine.Utilities;
#endif
using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Introductions.Introduced;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Linking;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.CodeModel.Comparers;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
#if ROSLYN_5_0_0_OR_GREATER
using System.Globalization;
#endif
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using RefKind = Metalama.Framework.Code.RefKind;
using TypeKind = Metalama.Framework.Code.TypeKind;
using VarianceKind = Metalama.Framework.Code.VarianceKind;

namespace Metalama.Framework.Engine.Pipeline.DesignTime
{
    internal static class DesignTimeSyntaxTreeGenerator
    {
        public static async Task<IReadOnlyCollection<IntroducedSyntaxTree>> GenerateDesignTimeSyntaxTreesAsync(
            ProjectServiceProvider serviceProvider,
            PartialCompilation partialCompilation,
            CompilationModel initialCompilationModel,
            CompilationModel finalCompilationModel,
            IEnumerable<ITransformation> transformations,
            UserDiagnosticSink diagnostics,
            TestableCancellationToken cancellationToken )
        {
            var additionalSyntaxTreeDictionary = new ConcurrentDictionary<string, IntroducedSyntaxTree>();

            var useNullability = partialCompilation.InitialCompilation.Options.NullableContextOptions != NullableContextOptions.Disable;

            var lexicalScopeFactory = new LexicalScopeFactory( finalCompilationModel );
            var injectionHelperProvider = new LinkerInjectionHelperProvider( finalCompilationModel, useNullability );
            var injectionNameProvider = new LinkerInjectionNameProvider( finalCompilationModel, injectionHelperProvider );
            var aspectReferenceSyntaxProvider = new LinkerAspectReferenceSyntaxProvider();

            // Get all transformations that are observable at design time and group them by future target file.
            var transformationsByBucket =
                transformations
                    .Where( t => t.Observability == TransformationObservability.Always )
                    .GroupBy(
                        t =>
                            t.TargetDeclaration switch
                            {
                                IFullRef<INamespace> @namespace => @namespace,
                                IFullRef<INamedType> namedType => namedType,
                                IFullRef<IMember> member => member.DeclaringType.AssertNotNull(),
                                _ => throw new AssertionFailedException( $"Unsupported: {t.TargetDeclaration}" )
                            },
                        RefEqualityComparer<INamespaceOrNamedType>.Default )
                    .ToDictionary( g => g.Key!, g => g.AsEnumerable(), RefEqualityComparer<INamespaceOrNamedType>.Default );

            var taskScheduler = serviceProvider.GetRequiredService<IConcurrentTaskRunner>();

            await taskScheduler.RunConcurrentlyAsync( transformationsByBucket, ProcessTransformationsOnTypeOrNamespace, cancellationToken );

            void ProcessTransformationsOnTypeOrNamespace( KeyValuePair<IRef<INamespaceOrNamedType>, IEnumerable<ITransformation>> transformationGroup )
            {
                var target = transformationGroup.Key.GetTarget( finalCompilationModel );

                switch ( target.DeclarationKind )
                {
                    case DeclarationKind.NamedType or DeclarationKind.ExtensionBlock when target is INamedType namedType:
                        ProcessTransformationsOnType( namedType, transformationGroup.Value );

                        break;

                    case DeclarationKind.Namespace:
                        ProcessTransformationsOnNamespace( transformationGroup.Value );

                        break;

                    default:
                        throw new AssertionFailedException( $"Unsupported: {transformationGroup.Key}" );
                }
            }

            void ProcessTransformationsOnNamespace( IEnumerable<ITransformation> namespaceTransformations )
            {
                cancellationToken.ThrowIfCancellationRequested();

                var orderedTransformations = namespaceTransformations.OrderBy( x => x, TransformationLinkerOrderComparer.Instance );

                foreach ( var transformation in orderedTransformations )
                {
                    if ( transformation is IIntroduceDeclarationTransformation
                         {
                             DeclarationBuilderData: NamedTypeBuilderData namedTypeBuilder
                         } introduceDeclarationTransformation
                         && !transformationsByBucket.ContainsKey(
                             introduceDeclarationTransformation.DeclarationBuilderData.ToFullRef().As<INamespaceOrNamedType>() ) )
                    {
                        // If this is an introduced type that does not have any transformations, we will "process" it to get the empty type.
                        ProcessTransformationsOnType( namedTypeBuilder.ToRef().GetTarget( finalCompilationModel ), Array.Empty<ITransformation>() );
                    }
                }
            }

            void ProcessTransformationsOnType( INamedType declaringTypeOrExtensionBlock, IEnumerable<ITransformation> typeTransformations )
            {
                cancellationToken.ThrowIfCancellationRequested();

                var extensionBlock = declaringTypeOrExtensionBlock as IExtensionBlock;
                var declaringType = extensionBlock?.DeclaringType ?? declaringTypeOrExtensionBlock;

                if ( declaringType is { IsPartial: false, Origin.Kind: not DeclarationOriginKind.Aspect } )
                {
                    // If the type is not marked as partial, we can emit a diagnostic and a code fix, but not a partial class itself.
                    diagnostics.Report(
                        GeneralDiagnosticDescriptors.TypeNotPartial.CreateRoslynDiagnostic( declaringType.GetDiagnosticLocation(), declaringType ) );

                    return;
                }

                if ( IsInNonPartialSourceType( declaringType ) )
                {
                    // If the declaring type is not located in a partial source type, we need to skip it. The warning is needed because it was done for the parent type.
                    return;
                }

                var orderedTransformations = typeTransformations.OrderBy( x => x, TransformationLinkerOrderComparer.Instance );

                // Process members.
                BaseListSyntax? baseList = null;

                var members = List<MemberDeclarationSyntax>();
                var syntaxGenerationContext = finalCompilationModel.CompilationContext.GetSyntaxGenerationContext( SyntaxGenerationOptions.Formatted, true );

                foreach ( var transformation in orderedTransformations )
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    switch ( transformation )
                    {
                        case IInjectMemberTransformation injectMemberTransformation:
                            // TODO: Provide other implementations or allow nulls (because this pipeline should not execute anything).
                            // TODO: Implement support for initializable transformations.
                            var introductionContext = new MemberInjectionContext(
                                serviceProvider,
                                diagnostics,
                                injectionNameProvider,
                                lexicalScopeFactory,
                                aspectReferenceSyntaxProvider,
                                syntaxGenerationContext,
                                finalCompilationModel );

                            var injectedMembers = injectMemberTransformation.GetInjectedMembers( introductionContext )
                                .Select( m => m.Syntax );

                            if ( injectMemberTransformation is IIntroduceDeclarationTransformation { DeclarationBuilderData: NamedTypeBuilderData } )
                            {
                                // TODO: This is not optimal - the injected member should be skipped instead.
                                //       However, determining whether the type should be injected as a member depends on transformations after this
                                //       one, so we would need two passes.
                                injectedMembers = AddPartialModifierToTypes( injectedMembers );
                            }

                            members = members.AddRange( injectedMembers );

                            break;

                        case IInjectInterfaceTransformation injectInterfaceTransformation:
                            baseList ??= BaseList();
                            baseList = baseList.AddTypes( injectInterfaceTransformation.GetSyntax( syntaxGenerationContext ) );

                            break;

                        case IntroduceParameterTransformation:
                            // Parameter introductions are processed by CreateInjectedConstructors but they still need to be observable.
                            break;

                        default:
                            throw new AssertionFailedException( $"Don't know how to process {transformation.GetType().Name} at design time." );
                    }
                }

                members = members.AddRange(
                    CreateInjectedConstructors( initialCompilationModel, finalCompilationModel, syntaxGenerationContext, declaringType ) );

#if ROSLYN_5_0_0_OR_GREATER

                // Create the extension block.
                if ( extensionBlock != null )
                {
                    var extensionBlockSyntax = CreateExtensionBlock( extensionBlock, members, syntaxGenerationContext );
                    members = List<MemberDeclarationSyntax>( [extensionBlockSyntax] );
                }
#endif

                // Create a type.
                var typeDeclaration = CreatePartialType( declaringType, baseList, members );

                // Add the type to its nesting type.
                var topDeclaration = (MemberDeclarationSyntax) typeDeclaration;

                for ( var containingType = declaringType.DeclaringType; containingType != null; containingType = containingType.DeclaringType )
                {
                    topDeclaration = CreatePartialType(
                        containingType,
                        null,
                        SingletonList( topDeclaration ) );
                }

                // Add the class to a namespace.
                if ( !declaringType.ContainingNamespace.IsGlobalNamespace )
                {
                    topDeclaration = NamespaceDeclaration(
                        ParseName( declaringType.ContainingNamespace.FullName ),
                        default,
                        default,
                        SingletonList( topDeclaration ) );
                }

                // Choose the best syntax tree
                var originalSyntaxTree = ((IDeclarationImpl) declaringType).DeclaringSyntaxReferences.Select( r => r.SyntaxTree )
                    .OrderBy( s => s.FilePath.Length )
                    .FirstOrDefault();

                var compilationUnit = CompilationUnit()
                    .WithMembers( SingletonList( AddHeader( topDeclaration ) ) );

                var generatedSyntaxTree = SyntaxTree( compilationUnit.NormalizeWhitespace(), encoding: Encoding.UTF8 );

                // Find a unique syntax tree name. 
                var safeTypeName = GetUniqueFilenameForType( declaringTypeOrExtensionBlock );
                var syntaxTreeName = safeTypeName + ".cs";

                if ( !additionalSyntaxTreeDictionary.TryAdd(
                        syntaxTreeName,
                        new IntroducedSyntaxTree( syntaxTreeName, originalSyntaxTree, generatedSyntaxTree ) ) )
                {
                    // It is essential that GetUniqueFilenameForType deterministically generates unique file names because
                    // we cannot add de-duplicating suffixes here because concurrency creates non-determinism.
                    throw new AssertionFailedException( $"The generated file name '{syntaxTreeName}' is not unique." );
                }
            }

            return additionalSyntaxTreeDictionary.Values.AsReadOnly();
        }

        private static string GetUniqueFilenameForType( INamedType type )
        {
            var sb = new StringBuilder();

            RenderName( sb, type );

            return sb.ToString();

            static void RenderName( StringBuilder sb, INamedType current )
            {
                if ( current.DeclaringType != null )
                {
                    RenderName( sb, current.DeclaringType );
                    sb.Append( "-" );
                }
                else if ( current.ContainingNamespace.FullName != "" )
                {
                    sb.Append( current.ContainingNamespace.FullName );
                    sb.Append( "." );
                }

#if ROSLYN_5_0_0_OR_GREATER
                if ( current.TypeKind == TypeKind.Extension )
                {
                    var primarySyntax = current.GetPrimaryDeclarationSyntax();

                    if ( primarySyntax != null )
                    {
                        // Source-defined extension block: use location-based hash
                        var location = primarySyntax.GetLocation();
                        using var hashHandle = HashUtilities.AllocateHasher();
                        var hash = hashHandle.Value;
                        hash.Append( location.SourceTree.AssertNotNull().FilePath );
                        hash.Append( location.SourceSpan.Start );
                        sb.AppendFormat( CultureInfo.InvariantCulture, "{0:x}", (int) hash.GetCurrentHashAsUInt64() );
                    }
                    else
                    {
                        // Introduced extension block: use the Name for deterministic naming.
                        sb.Append( current.Name );
                    }
                }
                else
#endif
                {
                    sb.Append( current.Name );

                    if ( current.IsGeneric )
                    {
                        sb.Append( "{" );
                        sb.Append( current.TypeParameters.Count );
                        sb.Append( "}" );
                    }
                }
            }
        }

        private static IEnumerable<MemberDeclarationSyntax> AddPartialModifierToTypes( IEnumerable<MemberDeclarationSyntax> injectedMembers )
        {
            foreach ( var member in injectedMembers )
            {
                if ( member.Kind() is SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration or SyntaxKind.InterfaceDeclaration or SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration
                     && member is TypeDeclarationSyntax typeDeclaration
                     && !typeDeclaration.Modifiers.Any( SyntaxKind.PartialKeyword ) )
                {
                    yield return
                        member.WithModifiers( member.Modifiers.Add( Token( TriviaList( ElasticSpace ), SyntaxKind.PartialKeyword, TriviaList() ) ) );
                }
                else
                {
                    yield return member;
                }
            }
        }

        private static IEnumerable<ConstructorDeclarationSyntax> CreateInjectedConstructors(
            CompilationModel initialCompilationModel,
            CompilationModel finalCompilationModel,
            SyntaxGenerationContext syntaxGenerationContext,
            INamedType type )
        {
            // TODO: This will not work properly with universal constructor builders.
            var initialType = type.Translate( initialCompilationModel );
            var finalType = type.Translate( finalCompilationModel );

            var constructors = new List<ConstructorDeclarationSyntax>();
            var existingSignatures = new HashSet<(IType Type, RefKind RefKind)[]>( ConstructorSignatureEqualityComparer.Instance );

            // Go through all types that will get generated constructors and index existing constructors.
            foreach ( var constructor in initialType.Constructors )
            {
                existingSignatures.Add(
                    constructor.Parameters.SelectAsArray(
                        p => (p.Type, p.RefKind) ) );
            }

            // Additionally, add all introduced constructors to the list.
            foreach ( var introducedConstructor in finalType.Constructors.Where( c => c.Origin is { Kind: DeclarationOriginKind.Aspect } ) )
            {
                if ( (introducedConstructor.ToRef() as IIntroducedRef)?.BuilderData is not ConstructorBuilderData { ReplacedImplicitConstructor: null } )
                {
                    // Skip introduced constructors that are replacements.
                    continue;
                }

                existingSignatures.Add(
                    introducedConstructor.Parameters
                        .SelectAsArray(
                            p => (p.Type, p.RefKind) ) );
            }

            foreach ( var constructor in type.Constructors )
            {
                if ( !constructor.TryForCompilation( initialCompilationModel, out var initialConstructor ) )
                {
                    continue;
                }

                if ( initialConstructor is IntroducedDeclaration )
                {
                    continue;
                }

                var finalConstructor = constructor.Translate( finalCompilationModel );

                // Note that ParameterBuilder.Parameter does not include parameters added by advice, so we
                // must see the final parameters in the final compilation.
                var finalParameters = finalConstructor.Parameters.ToImmutableArray();
                var initialParameters = initialConstructor.Parameters.ToImmutableArray();

                var finalSignature = finalParameters.SelectAsArray(
                    p => (p.Type, p.RefKind) );

                if ( !existingSignatures.Add( finalSignature ) )
                {
                    continue;
                }

                // For generated constructors, exclude the Partial modifier. C# 14 partial constructors require matching
                // definition and implementation parts, but design-time generated constructors are standalone.
                constructors.Add(
                    ConstructorDeclaration(
                        List<AttributeListSyntax>(),
                        finalConstructor.GetSyntaxModifierList( ModifierCategories.All & ~ModifierCategories.Partial ),
                        SyntaxFactoryEx.SafeIdentifier( finalConstructor.DeclaringType.Name ),
                        syntaxGenerationContext.SyntaxGenerator.ParameterList( finalParameters, initialCompilationModel ),
                        initialConstructor.IsImplicitlyDeclared
                            ? null
                            : ConstructorInitializer(
                                SyntaxKind.ThisConstructorInitializer,
                                ArgumentList(
                                    SeparatedList(
                                        initialParameters.SelectAsArray(
                                            p =>
                                                Argument(
                                                    p.DefaultValue != null
                                                        ? NameColon( p.Name )
                                                        : null,
                                                    GetArgumentRefToken( p ),
                                                    SyntaxFactoryEx.SafeIdentifierName( p.Name ) ) ) ) ) ),
                        Block() ) );

                if ( initialConstructor.Parameters.Any( p => p.DefaultValue != null ) )
                {
                    // Target constructor has optional parameters.
                    // If there is no constructor without optional parameters, we need to generate it to avoid ambiguous match.

                    var nonOptionalParameters = initialParameters.Where( p => p.DefaultValue == null ).ToArray();
                    var optionalParameters = initialParameters.Where( p => p.DefaultValue != null ).ToArray();

                    if ( existingSignatures.Add(
                            nonOptionalParameters.SelectAsArray(
                                p => (p.Type, p.RefKind) ) ) )
                    {
                        constructors.Add(
                            ConstructorDeclaration(
                                List<AttributeListSyntax>(),
                                initialConstructor.GetSyntaxModifierList( ModifierCategories.All & ~ModifierCategories.Partial ),
                                SyntaxFactoryEx.SafeIdentifier( initialConstructor.DeclaringType.Name ),
                                syntaxGenerationContext.SyntaxGenerator.ParameterList( nonOptionalParameters, initialCompilationModel ),
                                ConstructorInitializer(
                                    SyntaxKind.ThisConstructorInitializer,
                                    ArgumentList(
                                        SeparatedList(
                                                nonOptionalParameters.SelectAsArray(
                                                    p => Argument( null, GetArgumentRefToken( p ), SyntaxFactoryEx.SafeIdentifierName( p.Name ) ) ) )
                                            .AddRange(
                                                optionalParameters.SelectAsArray(
                                                    p =>
                                                        Argument(
                                                            NameColon( p.Name ),
                                                            GetArgumentRefToken( p ),
                                                            DefaultExpression( syntaxGenerationContext.SyntaxGenerator.TypeSyntax( p.Type ) ) ) ) ) ) ),
                                Block() ) );
                    }
                }
            }

            return constructors;

            static SyntaxToken GetArgumentRefToken( IParameter p )
            {
                return p.RefKind switch
                {
                    RefKind.None or RefKind.In => default,
                    RefKind.Ref or RefKind.RefReadOnly => Token( SyntaxKind.RefKeyword ),
                    RefKind.Out => Token( SyntaxKind.OutKeyword ),
                    _ => throw new AssertionFailedException( $"Unsupported: {p.RefKind}" )
                };
            }
        }

#if ROSLYN_5_0_0_OR_GREATER
        private static ExtensionBlockDeclarationSyntax CreateExtensionBlock(
            IExtensionBlock extensionBlock,
            SyntaxList<MemberDeclarationSyntax> members,
            SyntaxGenerationContext syntaxGenerationContext )
        {
            return ExtensionBlockDeclaration(
                attributeLists: default,
                modifiers: default,
                Token( SyntaxKind.ExtensionKeyword ),
                CreateTypeParameters( extensionBlock ),
                CreateExtensionBlockParameterList( extensionBlock, syntaxGenerationContext ),
                CreateConstraintList( extensionBlock, syntaxGenerationContext ),
                Token( SyntaxKind.OpenBraceToken ),
                members,
                Token( SyntaxKind.CloseBraceToken ),
                default );
        }

        private static SyntaxList<TypeParameterConstraintClauseSyntax> CreateConstraintList(
            IExtensionBlock extensionBlock,
            SyntaxGenerationContext syntaxGenerationContext )
        {
            return syntaxGenerationContext.SyntaxGenerator.ConstraintClauses( extensionBlock );
        }

        private static ParameterListSyntax CreateExtensionBlockParameterList( IExtensionBlock extensionBlock, SyntaxGenerationContext syntaxGenerationContext )
        {
            return syntaxGenerationContext.SyntaxGenerator.ParameterList( [extensionBlock.ReceiverParameter], (CompilationModel) extensionBlock.Compilation );
        }

#endif

        private static TypeDeclarationSyntax CreatePartialType( INamedType type, BaseListSyntax? baseList, SyntaxList<MemberDeclarationSyntax> members )
            => type.TypeKind switch
            {
                TypeKind.Class when !type.IsRecord => ClassDeclaration(
                    attributeLists: default,
                    type.IsStatic
                        ? TokenList(
                            Token( TriviaList(), SyntaxKind.StaticKeyword, TriviaList( ElasticSpace ) ),
                            Token( SyntaxKind.PartialKeyword ) )
                        : SyntaxTokenList.Create( Token( SyntaxKind.PartialKeyword ) ),
                    SyntaxFactoryEx.SafeIdentifier( type.Name ),
                    CreateTypeParameters( type ),
                    baseList,
                    constraintClauses: default,
                    members ),
                TypeKind.Class when type.IsRecord => RecordDeclaration(
                    SyntaxKind.RecordDeclaration,
                    attributeLists: default,
                    SyntaxTokenList.Create( Token( SyntaxKind.PartialKeyword ) ),
                    keyword: Token( SyntaxKind.RecordKeyword ),
                    classOrStructKeyword: Token( SyntaxKind.ClassKeyword ),
                    SyntaxFactoryEx.SafeIdentifier( type.Name ),
                    CreateTypeParameters( type ),
                    parameterList: null,
                    baseList,
                    constraintClauses: default,
                    openBraceToken: Token( SyntaxKind.OpenBraceToken ),
                    members,
                    closeBraceToken: Token( SyntaxKind.CloseBraceToken ),
                    semicolonToken: default ),
                TypeKind.Struct when !type.IsRecord => StructDeclaration(
                    attributeLists: default,
                    SyntaxTokenList.Create( Token( SyntaxKind.PartialKeyword ) ),
                    SyntaxFactoryEx.SafeIdentifier( type.Name ),
                    CreateTypeParameters( type ),
                    baseList,
                    constraintClauses: default,
                    members ),
                TypeKind.Struct when type.IsRecord => RecordDeclaration(
                    SyntaxKind.RecordStructDeclaration,
                    attributeLists: default,
                    SyntaxTokenList.Create( Token( SyntaxKind.PartialKeyword ) ),
                    keyword: Token( SyntaxKind.RecordKeyword ),
                    classOrStructKeyword: Token( SyntaxKind.StructKeyword ),
                    SyntaxFactoryEx.SafeIdentifier( type.Name ),
                    CreateTypeParameters( type ),
                    parameterList: null,
                    baseList,
                    constraintClauses: default,
                    openBraceToken: Token( SyntaxKind.OpenBraceToken ),
                    members,
                    closeBraceToken: Token( SyntaxKind.CloseBraceToken ),
                    semicolonToken: default ),
                TypeKind.Interface => InterfaceDeclaration(
                    attributeLists: default,
                    SyntaxTokenList.Create( Token( SyntaxKind.PartialKeyword ) ),
                    SyntaxFactoryEx.SafeIdentifier( type.Name ),
                    CreateTypeParameters( type ),
                    baseList,
                    constraintClauses: default,
                    members ),
                _ => throw new AssertionFailedException( $"Unknown type kind: {type.TypeKind}." )
            };

        private static TypeParameterListSyntax? CreateTypeParameters( INamedType type )
        {
            if ( !type.IsGeneric )
            {
                return null;
            }

            static SyntaxKind GetVariance( VarianceKind variance )
            {
                return variance switch
                {
                    VarianceKind.None => SyntaxKind.None,
                    VarianceKind.In => SyntaxKind.InKeyword,
                    VarianceKind.Out => SyntaxKind.OutKeyword,
                    _ => throw new AssertionFailedException( $"Unknown variance: {variance}." )
                };
            }

            return TypeParameterList(
                SeparatedList(
                    type.TypeParameters.SelectAsReadOnlyList( tp => TypeParameter( tp.Name ).WithVarianceKeyword( Token( GetVariance( tp.Variance ) ) ) ) ) );
        }

        private static MemberDeclarationSyntax AddHeader( MemberDeclarationSyntax node )
            => node switch
            {
                NamespaceDeclarationSyntax or ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax or InterfaceDeclarationSyntax =>
                    node.WithLeadingTrivia( GetHeader() ),
                _ => node
            };

        private static SyntaxTriviaList GetHeader()
        {
            const string generatedByMetalama = " Generated by Metalama to support the code editing experience. This is NOT the code that gets executed.";

            return TriviaList(
                Trivia(
                    DocumentationCommentTrivia(
                        SyntaxKind.SingleLineDocumentationCommentTrivia,
                        List(
                            new XmlNodeSyntax[]
                            {
                                XmlText()
                                    .WithTextTokens(
                                        TokenList(
                                            XmlTextLiteral(
                                                TriviaList( DocumentationCommentExterior( "///" ) ),
                                                " ",
                                                " ",
                                                TriviaList() ) ) ),
                                XmlExampleElement(
                                        SingletonList<XmlNodeSyntax>(
                                            XmlText()
                                                .WithTextTokens(
                                                    TokenList(
                                                        XmlTextNewLine(
                                                            TriviaList(),
                                                            "\n",
                                                            "\n",
                                                            TriviaList() ),
                                                        XmlTextLiteral(
                                                            TriviaList( DocumentationCommentExterior( "///" ) ),
                                                            generatedByMetalama,
                                                            generatedByMetalama,
                                                            TriviaList() ),
                                                        XmlTextNewLine(
                                                            TriviaList(),
                                                            "\n",
                                                            "\n",
                                                            TriviaList() ),
                                                        XmlTextLiteral(
                                                            TriviaList( DocumentationCommentExterior( "///" ) ),
                                                            " ",
                                                            " ",
                                                            TriviaList() ) ) ) ) )
                                    .WithStartTag( XmlElementStartTag( XmlName( Identifier( "auto-generated" ) ) ) )
                                    .WithEndTag(
                                        XmlElementEndTag( XmlName( Identifier( "auto-generated" ) ) )
                                            .WithGreaterThanToken( Token( SyntaxKind.GreaterThanToken ) ) ),
                                XmlText()
                                    .WithTextTokens(
                                        TokenList(
                                            XmlTextNewLine(
                                                TriviaList(),
                                                "\n",
                                                "\n",
                                                TriviaList() ) ) )
                            } ) ) ),
                LineFeed,
                LineFeed );
        }

        private sealed class ConstructorSignatureEqualityComparer : IEqualityComparer<(IType Type, RefKind RefKind)[]>
        {
            public static ConstructorSignatureEqualityComparer Instance { get; } = new();

            private readonly SignatureTypeComparer _typeComparer = SignatureTypeComparer.Instance;

            private ConstructorSignatureEqualityComparer() { }

            public bool Equals( (IType Type, RefKind RefKind)[]? x, (IType Type, RefKind RefKind)[]? y )
            {
                if ( x == null || y == null )
                {
                    return x == null && y == null;
                }

                if ( x.Length != y.Length )
                {
                    return false;
                }

                return this.Equals( x, y, x.Length );
            }

            private bool Equals( (IType Type, RefKind RefKind)[] x, (IType Type, RefKind RefKind)[] y, int count )
            {
                for ( var i = 0; i < count; i++ )
                {
                    if ( x[i].RefKind != y[i].RefKind )
                    {
                        return false;
                    }

                    if ( !this._typeComparer.Equals( x[i].Type, y[i].Type ) )
                    {
                        return false;
                    }
                }

                return true;
            }

            public int GetHashCode( (IType Type, RefKind RefKind)[] obj )
            {
                var hashCode = obj.Length;

                for ( var i = 0; i < obj.Length; i++ )
                {
                    hashCode = HashCode.Combine( hashCode, this._typeComparer.GetHashCode( obj[i].Type ), (int) obj[i].RefKind );
                }

                return hashCode;
            }
        }

        private static bool IsInNonPartialSourceType( INamedType declaringType )
        {
            var currentType = declaringType;

            // Go to the closest type that does not originate in an aspect.
            while ( currentType.Origin.Kind is DeclarationOriginKind.Aspect && currentType.DeclaringType != null )
            {
                currentType = currentType.DeclaringType;
            }

            return currentType.Origin.Kind is not DeclarationOriginKind.Aspect && !currentType.IsPartial;
        }
    }
}