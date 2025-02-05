// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Utilities.Caching;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using System.Text;
using RefKind = Metalama.Framework.Code.RefKind;
using SpecialType = Microsoft.CodeAnalysis.SpecialType;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.SyntaxSerialization;

internal static class ReflectionSignatureBuilder
{
    public static bool HasTypeArgument( IMethod method )
        => TypeArgumentDetector.Instance.Visit( method.ReturnType.GetSymbol() )
           || method.TypeParameters.Count > 0
           || method.Parameters.Any( p => TypeArgumentDetector.Instance.Visit( p.Type.GetSymbol() ) );

    public static bool HasTypeArgument( IConstructor constructor )
        => constructor.Parameters.Any( p => p.Type.TypeKind == TypeKind.TypeParameter )
           || constructor.Parameters.Any( p => TypeArgumentDetector.Instance.Visit( p.Type.GetSymbol() ) );

    public static string GetMethodSignature( IMethod method )
    {
        using var impl = new StringBuildingVisitor();
        impl.BuildSignature( method );

        return impl.ToString();
    }

    public static string GetConstructorSignature( IConstructor constructor )
    {
        using var impl = new StringBuildingVisitor();
        impl.BuildSignature( constructor );

        return impl.ToString();
    }

    private sealed class TypeArgumentDetector : SymbolVisitor<bool>
    {
        public static TypeArgumentDetector Instance { get; } = new();

        public override bool VisitTypeParameter( ITypeParameterSymbol symbol ) => true;

        public override bool DefaultVisit( ISymbol symbol ) => throw new AssertionFailedException( $"No visitor implemented for {symbol.Kind}." );

        public override bool VisitNamedType( INamedTypeSymbol symbol ) => symbol.TypeArguments.Any( this.Visit );

        public override bool VisitArrayType( IArrayTypeSymbol symbol ) => this.Visit( symbol.ElementType );

        public override bool VisitDynamicType( IDynamicTypeSymbol symbol ) => false;

        public override bool VisitPointerType( IPointerTypeSymbol symbol ) => this.Visit( symbol.PointedAtType );

        public override bool VisitFunctionPointerType( IFunctionPointerTypeSymbol symbol ) => throw new NotImplementedException();
    }

    private sealed class StringBuildingVisitor : SymbolVisitor, IDisposable
    {
        private readonly ObjectPoolHandle<StringBuilder> _stringBuilderHandle = StringBuilderPool.Default.Allocate();

        private StringBuilder StringBuilder => this._stringBuilderHandle.Value;

        private bool _isTypeArgument;

        public void BuildSignature( IMethod method )
        {
            this.Visit( method.ReturnType.GetSymbol() );
            this.StringBuilder.Append( ' ' );
            this.StringBuilder.Append( method.Name );

            if ( method.TypeParameters.Count > 0 )
            {
                this.StringBuilder.Append( '[' );

                foreach ( var typeParameter in method.TypeParameters )
                {
                    if ( typeParameter.Index > 0 )
                    {
                        this.StringBuilder.Append( ',' );
                    }

                    this.StringBuilder.Append( typeParameter.Name );
                }

                this.StringBuilder.Append( ']' );
            }

            this.StringBuilder.Append( '(' );

            foreach ( var parameter in method.Parameters )
            {
                if ( parameter.Index > 0 )
                {
                    this.StringBuilder.Append( ", " );
                }

                this.Visit( parameter.Type.GetSymbol() );

                if ( parameter.RefKind != RefKind.None )
                {
                    this.StringBuilder.Append( " ByRef" );
                }
            }

            this.StringBuilder.Append( ')' );
        }

        public void BuildSignature( IConstructor constructor )
        {
            this.StringBuilder.Append( "Void " );
            this.StringBuilder.Append( constructor.Name );

            this.StringBuilder.Append( '(' );

            foreach ( var parameter in constructor.Parameters )
            {
                if ( parameter.Index > 0 )
                {
                    this.StringBuilder.Append( ", " );
                }

                this.Visit( parameter.Type.GetSymbol() );

                if ( parameter.RefKind != RefKind.None )
                {
                    this.StringBuilder.Append( " ByRef" );
                }
            }

            this.StringBuilder.Append( ')' );
        }

        public override string ToString() => this.StringBuilder.ToString();

        public override void DefaultVisit( ISymbol symbol ) => throw new NotSupportedException();

        public override void VisitArrayType( IArrayTypeSymbol symbol )
        {
            this.Visit( symbol.ElementType );
            this.StringBuilder.Append( '[' );

            for ( var i = 1; i < symbol.Rank; i++ )
            {
                this.StringBuilder.Append( ',' );
            }

            this.StringBuilder.Append( ']' );
        }

        public override void VisitDynamicType( IDynamicTypeSymbol symbol ) => this.StringBuilder.Append( "System.Object" );

        public override void VisitNamedType( INamedTypeSymbol symbol )
        {
            var requiresNamespace = this._isTypeArgument || symbol.SpecialType switch
            {
                SpecialType.None => true,
                SpecialType.System_Object => true,
                SpecialType.System_Enum => true,
                SpecialType.System_MulticastDelegate => true,
                SpecialType.System_Delegate => true,
                SpecialType.System_ValueType => true,
                SpecialType.System_Void => false,
                SpecialType.System_Boolean => false,
                SpecialType.System_Char => false,
                SpecialType.System_SByte => false,
                SpecialType.System_Byte => false,
                SpecialType.System_Int16 => false,
                SpecialType.System_UInt16 => false,
                SpecialType.System_Int32 => false,
                SpecialType.System_UInt32 => false,
                SpecialType.System_Int64 => false,
                SpecialType.System_UInt64 => false,
                SpecialType.System_Decimal => true,
                SpecialType.System_Single => true,
                SpecialType.System_Double => false,
                SpecialType.System_String => true,
                SpecialType.System_IntPtr => true,
                SpecialType.System_UIntPtr => true,
                SpecialType.System_Array => true,
                _ => true
            };

            if ( requiresNamespace && !symbol.ContainingNamespace.IsGlobalNamespace )
            {
                this.VisitNamespace( symbol.ContainingNamespace );
                this.StringBuilder.Append( '.' );
            }

            this.StringBuilder.Append( symbol.MetadataName );

            if ( symbol.TypeArguments.Length > 0 )
            {
                var oldIsTypeArgument = this._isTypeArgument;
                this._isTypeArgument = true;
                this.StringBuilder.Append( '[' );

                for ( var i = 0; i < symbol.TypeArguments.Length; i++ )
                {
                    var typeArgument = symbol.TypeArguments[i];

                    if ( i > 0 )
                    {
                        this.StringBuilder.Append( ',' );
                    }

                    this.Visit( typeArgument );
                }

                this._isTypeArgument = oldIsTypeArgument;
                this.StringBuilder.Append( ']' );
            }
        }

        public override void VisitNamespace( INamespaceSymbol symbol )
        {
            if ( symbol.IsGlobalNamespace )
            {
                throw new AssertionFailedException( "Cannot visit the global namespace." );
            }

            if ( !symbol.ContainingNamespace.IsGlobalNamespace )
            {
                this.VisitNamespace( symbol.ContainingNamespace );
                this.StringBuilder.Append( '.' );
            }

            this.StringBuilder.Append( symbol.Name );
        }

        public override void VisitPointerType( IPointerTypeSymbol symbol )
        {
            this.Visit( symbol.PointedAtType );
            this.StringBuilder.Append( '*' );
        }

        public override void VisitFunctionPointerType( IFunctionPointerTypeSymbol symbol ) => throw new NotImplementedException();

        public override void VisitTypeParameter( ITypeParameterSymbol symbol ) => this.StringBuilder.Append( symbol.Name );

        public void Dispose() => this._stringBuilderHandle.Dispose();
    }
}