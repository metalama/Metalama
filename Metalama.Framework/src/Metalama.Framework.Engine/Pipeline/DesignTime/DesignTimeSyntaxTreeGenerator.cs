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

            // Reuse the compilation's parse options so generated trees share LangVersion / preprocessor symbols with the
            // existing sources; otherwise Roslyn rejects them with "Inconsistent language versions".
            var parseOptions = partialCompilation.InitialCompilation.SyntaxTrees.FirstOrDefault()?.Options as CSharpParseOptions
                               ?? CSharpParseOptions.Default;

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

                // Compute the indent level of the innermost type's members, so injected members can be
                // indented and brace placement can match the surrounding nesting depth without a
                // NormalizeWhitespace pass.
                var typeDepth = 0;

                for ( var t = declaringType.DeclaringType; t != null; t = t.DeclaringType )
                {
                    typeDepth++;
                }

                if ( !declaringType.ContainingNamespace.IsGlobalNamespace )
                {
                    typeDepth++;
                }

#if ROSLYN_5_0_0_OR_GREATER
                var hasExtensionBlock = extensionBlock != null;
#else
                const bool hasExtensionBlock = false;
#endif

                // When an extension block wraps the members, they are children of the extension block,
                // which is itself a member of declaringType. Members therefore need an extra indent level.
                var memberIndentLevel = typeDepth + ( hasExtensionBlock ? 2 : 1 );

                // Each member's leading trivia receives a newline + indent so the member sits on its own
                // line at the right column. The members themselves are constructed with the rest of their
                // trivia (accessor list spacing, body brace placement) by the IInjectMemberTransformation
                // implementations and the FormattedBlock helper, so no further rewriting is needed here.
                var indentedMembers = new MemberDeclarationSyntax[members.Count];

                for ( var i = 0; i < members.Count; i++ )
                {
                    indentedMembers[i] = AddLeadingNewLineAndIndent( members[i], syntaxGenerationContext, memberIndentLevel );
                }

                members = List( indentedMembers );

#if ROSLYN_5_0_0_OR_GREATER

                // Create the extension block. The block's own indent level matches the depth at which
                // it appears (i.e., as a member of declaringType): typeDepth + 1.
                if ( extensionBlock != null )
                {
                    var extensionBlockDepth = typeDepth + 1;

                    var extensionBlockSyntax = CreateExtensionBlock( extensionBlock, members, syntaxGenerationContext, extensionBlockDepth );

                    // Indent the extension block declaration so it lines up with declaringType's body.
                    extensionBlockSyntax = (ExtensionBlockDeclarationSyntax) AddLeadingNewLineAndIndent(
                        extensionBlockSyntax,
                        syntaxGenerationContext,
                        extensionBlockDepth );

                    members = List<MemberDeclarationSyntax>( [extensionBlockSyntax] );
                }
#endif

                // Create a type. The wrapper tokens (keywords, braces) are constructed with explicit elastic
                // trivia so the resulting tree's ToFullString is parseable C# without a per-type
                // NormalizeWhitespace pass.
                var typeDeclaration = CreatePartialType( declaringType, baseList, members, syntaxGenerationContext, typeDepth );

                // Add the type to its nesting type.
                var topDeclaration = (MemberDeclarationSyntax) typeDeclaration;
                var currentDepth = typeDepth;

                for ( var containingType = declaringType.DeclaringType; containingType != null; containingType = containingType.DeclaringType )
                {
                    // The current top declaration becomes a member of containingType, so it sits at
                    // the indent level matching the containing type's body.
                    topDeclaration = AddLeadingNewLineAndIndent( topDeclaration, syntaxGenerationContext, currentDepth );
                    currentDepth--;

                    topDeclaration = CreatePartialType(
                        containingType,
                        null,
                        SingletonList( topDeclaration ),
                        syntaxGenerationContext,
                        currentDepth );
                }

                // Add the class to a namespace.
                if ( !declaringType.ContainingNamespace.IsGlobalNamespace )
                {
                    // Indent the type as a member of the namespace (depth 1 below the namespace keyword).
                    topDeclaration = AddLeadingNewLineAndIndent( topDeclaration, syntaxGenerationContext, 1 );

                    topDeclaration = NamespaceDeclaration(
                        SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.NamespaceKeyword ),
                        ParseName( declaringType.ContainingNamespace.FullName ),
                        Token( syntaxGenerationContext.OptionalElasticEndOfLineTriviaList, SyntaxKind.OpenBraceToken, default ),
                        externs: default,
                        usings: default,
                        SingletonList( topDeclaration ),
                        Token( syntaxGenerationContext.OptionalElasticEndOfLineTriviaList, SyntaxKind.CloseBraceToken, syntaxGenerationContext.OptionalElasticEndOfLineTriviaList ),
                        semicolonToken: default );
                }

                // Choose the best syntax tree
                var originalSyntaxTree = ((IDeclarationImpl) declaringType).DeclaringSyntaxReferences.Select( r => r.SyntaxTree )
                    .OrderBy( s => s.FilePath.Length )
                    .FirstOrDefault();

                var compilationUnit = CompilationUnit()
                    .WithMembers( SingletonList( AddHeader( topDeclaration ) ) );

                // Find a unique syntax tree name.
                var safeTypeName = GetUniqueFilenameForType( declaringTypeOrExtensionBlock );
                var syntaxTreeName = safeTypeName + ".cs";

                // No NormalizeWhitespace pass: the wrappers above and the IInjectMemberTransformation outputs
                // already carry the elastic trivia they need to render as parseable C#. Avoiding the per-type
                // pass is the whole point of this generator's perf improvement (issue #1601).
                var generatedSyntaxTree = CSharpSyntaxTree.Create(
                    compilationUnit,
                    parseOptions,
                    syntaxTreeName,
                    Encoding.UTF8 );

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

        // Returns "<newline><indent>" trivia for the requested indent level, using elastic newline so the
        // tree remains compatible with later formatting if it ever runs again, and explicit whitespace for
        // the indentation columns. When the syntax-generation context will not textualize the tree,
        // returns an empty trivia list (matching OptionalElasticEndOfLineTriviaList semantics).
        private static SyntaxTriviaList GetNewLineAndIndentTriviaList( SyntaxGenerationContext context, int level )
        {
            if ( context.OptionalElasticEndOfLineTriviaList.Count == 0 )
            {
                return default;
            }

            if ( level <= 0 )
            {
                return context.OptionalElasticEndOfLineTriviaList;
            }

            return TriviaList( ElasticEndOfLine( context.EndOfLine ), Whitespace( new string( ' ', level * 4 ) ) );
        }

        // Adds "<newline><indent>" before the existing leading trivia of the member, so the member sits on
        // its own line at the requested indent column.
        private static MemberDeclarationSyntax AddLeadingNewLineAndIndent(
            MemberDeclarationSyntax member,
            SyntaxGenerationContext context,
            int level )
        {
            var newLeading = GetNewLineAndIndentTriviaList( context, level );

            if ( newLeading.Count == 0 )
            {
                return member;
            }

            return member.WithLeadingTrivia( newLeading.AddRange( member.GetLeadingTrivia() ) );
        }

        private static IEnumerable<MemberDeclarationSyntax> AddPartialModifierToTypes( IEnumerable<MemberDeclarationSyntax> injectedMembers )
        {
            foreach ( var member in injectedMembers )
            {
                if ( member.Kind() is SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration or SyntaxKind.InterfaceDeclaration
                         or SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration
                     && member is TypeDeclarationSyntax typeDeclaration
                     && !typeDeclaration.Modifiers.Any( SyntaxKind.PartialKeyword ) )
                {
                    yield return
                        member.WithModifiers( member.Modifiers.Add( Token( TriviaList( ElasticSpace ), SyntaxKind.PartialKeyword, TriviaList( ElasticSpace ) ) ) );
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
                existingSignatures.Add( constructor.Parameters.SelectAsArray( p => (p.Type, p.RefKind) ) );
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
                        .SelectAsArray( p => (p.Type, p.RefKind) ) );
            }

            // Empty body with proper trivia so the constructor renders on its own line: FormattedBlock
            // emits an open brace with elastic newlines on both sides and a close brace with a leading
            // elastic newline, which is enough to render Allman-style "()\n{\n\n}" without any
            // NormalizeWhitespace pass downstream.
            var emptyBody = syntaxGenerationContext.SyntaxGenerator.FormattedBlock();

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

                var finalSignature = finalParameters.SelectAsArray( p => (p.Type, p.RefKind) );

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
                        emptyBody ) );

                if ( initialConstructor.Parameters.Any( p => p.DefaultValue != null ) )
                {
                    // Target constructor has optional parameters.
                    // If there is no constructor without optional parameters, we need to generate it to avoid ambiguous match.

                    var nonOptionalParameters = initialParameters.Where( p => p.DefaultValue == null ).ToArray();
                    var optionalParameters = initialParameters.Where( p => p.DefaultValue != null ).ToArray();

                    if ( existingSignatures.Add( nonOptionalParameters.SelectAsArray( p => (p.Type, p.RefKind) ) ) )
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
                                emptyBody ) );
                    }
                }
            }

            return constructors;

            static SyntaxToken GetArgumentRefToken( IParameter p )
            {
                return p.RefKind switch
                {
                    RefKind.None or RefKind.In => default,
                    RefKind.Ref or RefKind.RefReadOnly => SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.RefKeyword ),
                    RefKind.Out => SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.OutKeyword ),
                    _ => throw new AssertionFailedException( $"Unsupported: {p.RefKind}" )
                };
            }
        }

#if ROSLYN_5_0_0_OR_GREATER
        private static ExtensionBlockDeclarationSyntax CreateExtensionBlock(
            IExtensionBlock extensionBlock,
            SyntaxList<MemberDeclarationSyntax> members,
            SyntaxGenerationContext syntaxGenerationContext,
            int blockDepth )
        {
            // Both braces use the same "newline + blockDepth*4 spaces" leading trivia so they line up
            // with the extension keyword's column once that line has its own leading indent applied.
            var blockLineLeading = GetNewLineAndIndentTriviaList( syntaxGenerationContext, blockDepth );

            return ExtensionBlockDeclaration(
                attributeLists: default,
                modifiers: default,
                SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.ExtensionKeyword ),
                CreateTypeParameters( extensionBlock ),
                CreateExtensionBlockParameterList( extensionBlock, syntaxGenerationContext ),
                CreateConstraintList( extensionBlock, syntaxGenerationContext ),
                Token( blockLineLeading, SyntaxKind.OpenBraceToken, default ),
                members,
                Token( blockLineLeading, SyntaxKind.CloseBraceToken, syntaxGenerationContext.OptionalElasticEndOfLineTriviaList ),
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

        private static TypeDeclarationSyntax CreatePartialType(
            INamedType type,
            BaseListSyntax? baseList,
            SyntaxList<MemberDeclarationSyntax> members,
            SyntaxGenerationContext syntaxGenerationContext,
            int typeDepth )
        {
            // The wrapping tokens (modifiers, type keyword, braces) are constructed with explicit trivia so
            // ToFullString produces parseable C# without a NormalizeWhitespace pass. Modifiers carry trailing
            // elastic spaces; the open brace carries a trailing newline; the close brace's leading trivia is
            // a newline followed by the indentation that matches typeDepth so the brace lines up with the
            // type's own indentation column. Members are expected to provide their own leading newline and
            // indentation (see FormatInjectedMember), so the open brace does not need to spray newlines.
            var modifiers = type.IsStatic
                ? TokenList(
                    SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.StaticKeyword ),
                    SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PartialKeyword ) )
                : SyntaxTokenList.Create( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PartialKeyword ) );

            var typeLineLeading = GetNewLineAndIndentTriviaList( syntaxGenerationContext, typeDepth );
            var openBrace = Token( typeLineLeading, SyntaxKind.OpenBraceToken, default );
            var closeBrace = Token( typeLineLeading, SyntaxKind.CloseBraceToken, syntaxGenerationContext.OptionalElasticEndOfLineTriviaList );

            return type.TypeKind switch
            {
                TypeKind.Class when !type.IsRecord => ClassDeclaration(
                    attributeLists: default,
                    modifiers,
                    keyword: SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.ClassKeyword ),
                    SyntaxFactoryEx.SafeIdentifier( type.Name ),
                    CreateTypeParameters( type ),
                    baseList,
                    constraintClauses: default,
                    openBraceToken: openBrace,
                    members,
                    closeBraceToken: closeBrace,
                    semicolonToken: default ),
                TypeKind.Class when type.IsRecord => RecordDeclaration(
                    SyntaxKind.RecordDeclaration,
                    attributeLists: default,
                    modifiers,
                    keyword: SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.RecordKeyword ),
                    classOrStructKeyword: SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.ClassKeyword ),
                    SyntaxFactoryEx.SafeIdentifier( type.Name ),
                    CreateTypeParameters( type ),
                    parameterList: null,
                    baseList,
                    constraintClauses: default,
                    openBraceToken: openBrace,
                    members,
                    closeBraceToken: closeBrace,
                    semicolonToken: default ),
                TypeKind.Struct when !type.IsRecord => StructDeclaration(
                    attributeLists: default,
                    modifiers,
                    keyword: SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.StructKeyword ),
                    SyntaxFactoryEx.SafeIdentifier( type.Name ),
                    CreateTypeParameters( type ),
                    baseList,
                    constraintClauses: default,
                    openBraceToken: openBrace,
                    members,
                    closeBraceToken: closeBrace,
                    semicolonToken: default ),
                TypeKind.Struct when type.IsRecord => RecordDeclaration(
                    SyntaxKind.RecordStructDeclaration,
                    attributeLists: default,
                    modifiers,
                    keyword: SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.RecordKeyword ),
                    classOrStructKeyword: SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.StructKeyword ),
                    SyntaxFactoryEx.SafeIdentifier( type.Name ),
                    CreateTypeParameters( type ),
                    parameterList: null,
                    baseList,
                    constraintClauses: default,
                    openBraceToken: openBrace,
                    members,
                    closeBraceToken: closeBrace,
                    semicolonToken: default ),
                TypeKind.Interface => InterfaceDeclaration(
                    attributeLists: default,
                    modifiers,
                    keyword: SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.InterfaceKeyword ),
                    SyntaxFactoryEx.SafeIdentifier( type.Name ),
                    CreateTypeParameters( type ),
                    baseList,
                    constraintClauses: default,
                    openBraceToken: openBrace,
                    members,
                    closeBraceToken: closeBrace,
                    semicolonToken: default ),
                _ => throw new AssertionFailedException( $"Unknown type kind: {type.TypeKind}." )
            };
        }

        private static TypeParameterListSyntax? CreateTypeParameters( INamedType type )
        {
            if ( !type.IsGeneric )
            {
                return null;
            }

            static SyntaxToken GetVarianceToken( VarianceKind variance )
            {
                // Variance keywords need a trailing space, otherwise `in`/`out` joins with the following
                // type-parameter identifier (e.g. `IRepository<inT>`) and produces invalid C#.
                return variance switch
                {
                    VarianceKind.None => default,
                    VarianceKind.In => SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.InKeyword ),
                    VarianceKind.Out => SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.OutKeyword ),
                    _ => throw new AssertionFailedException( $"Unknown variance: {variance}." )
                };
            }

            return TypeParameterList(
                SeparatedList(
                    type.TypeParameters.SelectAsReadOnlyList( tp => TypeParameter( tp.Name ).WithVarianceKeyword( GetVarianceToken( tp.Variance ) ) ) ) );
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