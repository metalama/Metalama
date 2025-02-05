// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Source;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Comparers;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using DeclarationKind = Metalama.Framework.Code.DeclarationKind;

namespace Metalama.Framework.Engine.CodeModel.References;

public static class RefExtensions
{
    internal static IFullRef<INamedType> ToFullRef( this INamedType declaration ) => (IFullRef<INamedType>) declaration.ToRef();

    internal static IFullRef<IMethod> ToFullRef( this IMethod declaration ) => (IFullRef<IMethod>) declaration.ToRef();

    internal static IFullRef<IField> ToFullRef( this IField declaration ) => (IFullRef<IField>) declaration.ToRef();

    internal static IFullRef<IProperty> ToFullRef( this IProperty declaration ) => (IFullRef<IProperty>) declaration.ToRef();

    internal static IFullRef<IIndexer> ToFullRef( this IIndexer declaration ) => (IFullRef<IIndexer>) declaration.ToRef();

    internal static IFullRef<IMember> ToFullRef( this IMember declaration ) => (IFullRef<IMember>) declaration.ToRef();

    internal static IFullRef<IParameter> ToFullRef( this IParameter declaration ) => (IFullRef<IParameter>) declaration.ToRef();

    internal static IFullRef<ITypeParameter> ToFullRef( this ITypeParameter declaration ) => (IFullRef<ITypeParameter>) declaration.ToRef();

    internal static IFullRef<IType> ToFullRef( this IType declaration ) => (IFullRef<IType>) declaration.ToRef();

    internal static IFullRef<IDeclaration> ToFullRef( this IDeclaration declaration ) => (IFullRef<IDeclaration>) declaration.ToRef();

    internal static IFullRef<IConstructor> ToFullRef( this IConstructor declaration ) => (IFullRef<IConstructor>) declaration.ToRef();

    internal static IFullRef<IEvent> ToFullRef( this IEvent declaration ) => (IFullRef<IEvent>) declaration.ToRef();

    internal static IFullRef<T> ToFullRef<T>( this IDeclaration compilationElement )
        where T : class, IDeclaration
        => (IFullRef<T>) compilationElement.ToRef();

    internal static IFullRef AsFullRef( this IRef reference ) => (IFullRef) reference;

    internal static IFullRef<T> AsFullRef<T>( this IRef<T> reference )
        where T : class, ICompilationElement
        => (IFullRef<T>) reference;

    /// <summary>
    /// Converts an <see cref="IDurableRef"/> to a <see cref="IFullRef{T}"/> given a <see cref="CompilationContext"/>.
    /// </summary>
    internal static IFullRef<T> ToFullRef<T>( this IRef<T> reference, RefFactory refFactory )
        where T : class, ICompilationElement
        => reference as IFullRef<T> ?? (IFullRef<T>) ((IDurableRef) reference).ToFullRef( refFactory );

    internal static IFullRef<T> AsFullRef<T>( this IRef reference )
        where T : class, ICompilationElement
        => (IFullRef<T>) reference.As<T>();

    internal static bool HasSymbol( this IRef reference ) => reference is ISymbolRef;

    internal static IDurableRef<T> ToDurable<T>( this IRef<T> reference )
        where T : class, ICompilationElement
        => (IDurableRef<T>) ((IRefImpl) reference).ToDurable();

    internal static IRef ToDurable( this IRef reference ) => ((IRefImpl) reference).ToDurable();

    internal static bool IsConvertibleTo( this IFullRef<IType> type, IType otherType, ConversionKind conversionKind = ConversionKind.Default )
        => type.ConstructedDeclaration.IsConvertibleTo( otherType, conversionKind );

    public static SyntaxTree? GetPrimarySyntaxTree( this IRef reference ) => ((IFullRef) reference).PrimarySyntaxTree;

    internal static Type[] GetPossibleDeclarationInterfaceTypes( this DeclarationKind declarationKind, RefTargetKind refTargetKind = RefTargetKind.Default )
        => refTargetKind switch
        {
            RefTargetKind.Return => [typeof(IParameter)],
            RefTargetKind.Assembly => [typeof(IAssembly)],
            RefTargetKind.Module => [typeof(IAssembly)],
            RefTargetKind.Field => [typeof(IField), typeof(IProperty)],
            RefTargetKind.Parameter => [typeof(IParameter)],
            RefTargetKind.Property => [typeof(IProperty)],
            RefTargetKind.Event => [typeof(IEvent)],
            RefTargetKind.PropertyGet => [typeof(IMethod)],
            RefTargetKind.PropertySet => [typeof(IMethod)],
            RefTargetKind.StaticConstructor => [typeof(IConstructor)],
            RefTargetKind.PropertySetParameter => [typeof(IParameter)],
            RefTargetKind.PropertyGetReturnParameter => [typeof(IParameter)],
            RefTargetKind.PropertySetReturnParameter => [typeof(IParameter)],
            RefTargetKind.EventRaise => [typeof(IMethod)],
            RefTargetKind.EventRaiseParameter => [typeof(IParameter)],
            RefTargetKind.EventRaiseReturnParameter => [typeof(IParameter)],
            RefTargetKind.NamedType => [typeof(INamedType)],
            RefTargetKind.Default => declarationKind switch
            {
                DeclarationKind.Type => [typeof(IType)],
                DeclarationKind.Compilation => [typeof(ICompilation)],
                DeclarationKind.NamedType => [typeof(INamedType)],
                DeclarationKind.Method or DeclarationKind.Operator or DeclarationKind.Finalizer => [typeof(IMethod)],
                DeclarationKind.Property => [typeof(IProperty), typeof(IField)],
                DeclarationKind.Indexer => [typeof(IIndexer)],
                DeclarationKind.Field => [typeof(IField), typeof(IProperty)],
                DeclarationKind.Event => [typeof(IEvent)],
                DeclarationKind.Parameter => [typeof(IParameter)],
                DeclarationKind.TypeParameter => [typeof(ITypeParameter)],
                DeclarationKind.Attribute => [typeof(IAttribute)],
                DeclarationKind.ManagedResource => [typeof(IManagedResource)],
                DeclarationKind.Constructor => [typeof(IConstructor)],
                DeclarationKind.AssemblyReference => [typeof(IAssembly)],
                DeclarationKind.Namespace => [typeof(INamespace)],
                _ => throw new ArgumentOutOfRangeException( nameof(declarationKind), declarationKind, null )
            },

            _ => throw new ArgumentOutOfRangeException( nameof(refTargetKind), refTargetKind, null )
        };

    internal static ISymbolRef<IDeclaration> ToRef( this ISymbol symbol, RefFactory refFactory ) => refFactory.FromDeclarationSymbol( symbol );

    internal static ISymbolRef<INamedType> ToRef( this INamedTypeSymbol symbol, RefFactory refFactory ) => refFactory.FromSymbol<INamedType>( symbol );

    internal static ISymbolRef<INamespace> ToRef( this INamespaceSymbol symbol, RefFactory refFactory ) => refFactory.FromSymbol<INamespace>( symbol );

    internal static ISymbolRef<IType> ToRef( this ITypeSymbol symbol, RefFactory refFactory, GenericContext? genericContext = null )
        => symbol.Kind switch
        {
            SymbolKind.TypeParameter => refFactory.FromSymbol<ITypeParameter>( symbol, genericContext ),
            SymbolKind.NamedType => refFactory.FromSymbol<INamedType>( symbol, genericContext ),
            _ => refFactory.FromSymbol<IType>( symbol, genericContext )
        };

    internal static IEqualityComparer<ISymbol> GetSymbolComparer(
        this RefComparison comparison,
        CompilationContext compilationContext1,
        CompilationContext compilationContext2 )
        => comparison.GetSymbolComparer( compilationContext1 == compilationContext2 );

    internal static IEqualityComparer<ISymbol> GetSymbolComparer( this RefComparison comparison, bool useReferential = false )
        => comparison switch
        {
            RefComparison.Default => SymbolEqualityComparer.Default,
            RefComparison.Structural when useReferential => SymbolEqualityComparer.Default,
            RefComparison.Structural => StructuralSymbolComparer.Default,
            RefComparison.StructuralIncludeNullability when useReferential => SymbolEqualityComparer.IncludeNullability,
            RefComparison.StructuralIncludeNullability => StructuralSymbolComparer.IncludeNullability,
            RefComparison.IncludeNullability => SymbolEqualityComparer.IncludeNullability,
            _ => throw new ArgumentOutOfRangeException()
        };

    private static ISymbol? GetOriginalSymbol( this IRef reference )
        => reference switch
        {
            ISymbolRef symbolRef => symbolRef.Symbol,
            IIntroducedRef { ReplacedDeclaration: { } replacedDeclaration } => replacedDeclaration.GetOriginalSymbol(),
            _ => null
        };

    public static ISymbol? GetOriginalSymbol( this IDeclaration declaration )
        => declaration switch
        {
            SymbolBasedDeclaration symbolBased => symbolBased.Symbol,
            _ => declaration.ToRef().GetOriginalSymbol()
        };

    internal static RefComparison ToRefComparison( this TypeComparison typeComparison )
        => typeComparison switch
        {
            TypeComparison.Default => RefComparison.Default,
            TypeComparison.IncludeNullability => RefComparison.IncludeNullability,
            _ => throw new ArgumentOutOfRangeException()
        };

    internal static TypeComparison ToTypeComparison( this RefComparison typeComparison )
        => typeComparison switch
        {
            RefComparison.Default => TypeComparison.Default,
            RefComparison.IncludeNullability => TypeComparison.IncludeNullability,
            _ => throw new ArgumentOutOfRangeException()
        };
}