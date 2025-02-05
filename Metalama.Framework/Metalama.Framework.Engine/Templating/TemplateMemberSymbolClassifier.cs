// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.CompileTime;
using Microsoft.CodeAnalysis;
using System.Linq;
using SymbolExtensions = Metalama.Framework.Engine.Utilities.Roslyn.SymbolExtensions;

namespace Metalama.Framework.Engine.Templating;

internal abstract class TemplateMemberSymbolClassifier
{
    public ISymbolClassifier SymbolClassifier { get; }

    protected TemplateMemberSymbolClassifier( ISymbolClassifier symbolClassifier )
    {
        this.SymbolClassifier = symbolClassifier;
    }

    public bool RequiresCompileTimeExecution( ISymbol? symbol )
        => symbol != null && this.SymbolClassifier.GetTemplatingScope( symbol ).GetExpressionExecutionScope()
            == TemplatingScope.CompileTimeOnly;

    public static bool IsDynamicParameter( ITypeSymbol? type )
        => type switch
        {
            null => false,
            IDynamicTypeSymbol => true,
            IArrayTypeSymbol { ElementType: IDynamicTypeSymbol } => true,
            IArrayTypeSymbol arrayType when IsDynamicParameter( arrayType.ElementType ) => true,
            INamedTypeSymbol { IsGenericType: true } genericType when genericType.TypeArguments.Any( IsDynamicParameter ) => true,
            _ => false
        };

    public static bool IsTemplateParameter( IParameterSymbol parameter )
        => parameter.ContainingSymbol is IMethodSymbol { MethodKind: not (MethodKind.LambdaMethod or MethodKind.LocalFunction) }
            or IPropertySymbol
            or IEventSymbol;

    public bool IsRunTimeTemplateParameter( IParameterSymbol parameter ) => IsTemplateParameter( parameter ) && !this.IsCompileTimeParameter( parameter );

    public static bool IsTemplateTypeParameter( ITypeParameterSymbol parameter )
        => parameter.ContainingSymbol is IMethodSymbol { MethodKind: not (MethodKind.LambdaMethod or MethodKind.LocalFunction) }
            or IPropertySymbol
            or IEventSymbol;

    public bool IsCompileTimeTemplateTypeParameter( ITypeParameterSymbol typeParameter )
        => IsTemplateTypeParameter( typeParameter ) && this.IsCompileTimeParameter( typeParameter );

    public bool IsRunTimeTemplateTypeParameter( ITypeParameterSymbol typeParameter )
        => IsTemplateTypeParameter( typeParameter ) && !this.IsCompileTimeParameter( typeParameter );

    public bool IsCompileTimeParameter( IParameterSymbol parameter )
        => this.SymbolClassifier.GetTemplatingScope( parameter ).GetExpressionExecutionScope() == TemplatingScope.CompileTimeOnly;

    public bool IsCompileTimeParameter( ITypeParameterSymbol parameter )
        => this.SymbolClassifier.GetTemplatingScope( parameter ).GetExpressionExecutionScope() == TemplatingScope.CompileTimeOnly;

    public static bool HasTemplateKeywordAttribute( ISymbol symbol )
        => symbol.GetAttributes()
            .Any( a => a.AttributeClass != null && SymbolExtensions.AnyBaseType( a.AttributeClass, t => t.Name == nameof(TemplateKeywordAttribute) ) );
}