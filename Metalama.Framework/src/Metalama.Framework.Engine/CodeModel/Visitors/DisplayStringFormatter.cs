// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Caching;
using System;
using System.Collections.Generic;
using System.Text;

namespace Metalama.Framework.Engine.CodeModel.Visitors;

// TODO: implement formatting based on CodeDisplayFormat
internal sealed class DisplayStringFormatter : CompilationElementVisitor
{
    private readonly GenericContext? _genericContext;
    private readonly CodeDisplayFormat _format;
    private readonly StringBuilder _stringBuilder;

    private static readonly Dictionary<SpecialType, string> _specialType = new()
    {
        [SpecialType.Boolean] = "bool",
        [SpecialType.Byte] = "byte",
        [SpecialType.Char] = "char",
        [SpecialType.Int16] = "short",
        [SpecialType.Double] = "double",
        [SpecialType.Int32] = "int",
        [SpecialType.Int64] = "long",
        [SpecialType.Object] = "object",
        [SpecialType.Single] = "float",
        [SpecialType.String] = "string",
        [SpecialType.Void] = "void",
        [SpecialType.SByte] = "sbyte",
        [SpecialType.UInt16] = "ushort",
        [SpecialType.UInt32] = "uint",
        [SpecialType.UInt64] = "ulong",
        [SpecialType.Decimal] = "decimal"
    };

    internal static string FormatSpecialType( SpecialType specialType ) => _specialType.TryGetValue( specialType, out var s ) ? s : specialType.ToString();

    private static readonly Dictionary<MethodKind, string> _methodKinds = new()
    {
        [MethodKind.EventAdd] = "add", [MethodKind.EventRemove] = "remove", [MethodKind.PropertyGet] = "get", [MethodKind.PropertySet] = "set"
    };

    private static readonly Dictionary<OperatorKind, string> _operators = new()
    {
        [OperatorKind.ImplicitConversion] = "implicit operator",
        [OperatorKind.ExplicitConversion] = "explicit operator",
        [OperatorKind.Addition] = "operator +",
        [OperatorKind.BitwiseAnd] = "operator &",
        [OperatorKind.BitwiseOr] = "operator |",
        [OperatorKind.Decrement] = "operator --",
        [OperatorKind.Division] = "operator /",
        [OperatorKind.Equality] = "operator ==",
        [OperatorKind.ExclusiveOr] = "operator ^",
        [OperatorKind.False] = "operator false",
        [OperatorKind.GreaterThan] = "operator >",
        [OperatorKind.GreaterThanOrEqual] = "operator >=",
        [OperatorKind.Increment] = "operator ++",
        [OperatorKind.Inequality] = "operator !=",
        [OperatorKind.LeftShift] = "operator <<",
        [OperatorKind.LessThan] = "operator <",
        [OperatorKind.LessThanOrEqual] = "operator <=",
        [OperatorKind.LogicalNot] = "operator !",
        [OperatorKind.Modulus] = "operator %",
        [OperatorKind.Multiply] = "operator *",
        [OperatorKind.OnesComplement] = "operator ~",
        [OperatorKind.RightShift] = "operator >>",
        [OperatorKind.Subtraction] = "operator -",
        [OperatorKind.True] = "operator true",
        [OperatorKind.UnaryNegation] = "operator -",
        [OperatorKind.UnaryPlus] = "operator +"
    };

    private DisplayStringFormatter( CodeDisplayFormat? format, GenericContext? genericContext, StringBuilder stringBuilder )
    {
        this._genericContext = genericContext;
        this._stringBuilder = stringBuilder;
        this._format = format ?? CodeDisplayFormat.ShortDiagnosticMessage;
    }

    public static string Format(
        ICompilationElement element,
        CodeDisplayFormat? format,
        [PublicAPI] CodeDisplayContext? context,
        GenericContext? genericContext = null )
    {
        using ( StackOverflowHelper.Detect() )
        {
            using var stringBuilderHandle = StringBuilderPool.Default.Allocate();
            var formatter = new DisplayStringFormatter( format, genericContext, stringBuilderHandle.Value );
            formatter.Visit( element );

            return formatter.ToString();
        }
    }

    public override string ToString() => this._stringBuilder.ToString();

    private void Append( string text ) => this._stringBuilder.Append( text );

    private void VisitParameterList( IParameterList parameterList )
    {
        for ( var index = 0; index < parameterList.Count; index++ )
        {
            if ( index > 0 )
            {
                this.Append( ", " );
            }

            var parameter = parameterList[index];

            if ( parameter.IsParams )
            {
                this.Append( "params " );
            }

            if ( parameter.IsThis )
            {
                this.Append( "this " );
            }

            switch ( parameter.RefKind )
            {
                case RefKind.None:
                    break;

                case RefKind.Out:
                    this.Append( "out " );

                    break;

                case RefKind.Ref:
                    this.Append( "ref " );

                    break;

                case RefKind.In:
                    this.Append( "in " );

                    break;

                case RefKind.RefReadOnly:
                    this.Append( "ref readonly " );

                    break;
            }

            this.Visit( parameter.Type );
        }
    }

    private void VisitTypeArgumentList( IReadOnlyList<IType> parameterList )
    {
        for ( var index = 0; index < parameterList.Count; index++ )
        {
            if ( index > 0 )
            {
                this.Append( ", " );
            }

            this.Visit( parameterList[index] );
        }
    }

    protected override void DefaultVisit( ICompilationElement element ) => throw new AssertionFailedException();

    public override void VisitNamespace( INamespace declaration ) => this.Append( declaration.FullName );

    public override void VisitAssemblyReference( IAssembly declaration ) => this.Append( declaration.Identity.ToString()! );

    public override void VisitConstructor( IConstructor declaration )
    {
        if ( this._format.IncludeParent )
        {
            this.Visit( declaration.DeclaringType );
            this.Append( "." );
        }

        this.Append( declaration.DeclaringType.Name );

        this.Append( "(" );
        this.VisitParameterList( declaration.Parameters );
        this.Append( ")" );
    }

    public override void VisitAttribute( IAttribute declaration )
    {
        this.Append( "[" );
        this.VisitConstructor( declaration.Constructor );
        this.Append( "]" );
    }

    private void PrintDeclarationName( IMember member )
    {
        if ( member.IsExplicitInterfaceImplementation )
        {
            var interfaceMember = member.GetExplicitInterfaceImplementation();
            this.Append( interfaceMember.DeclaringType.Name );
            this.Append( "." );
            this.Append( interfaceMember.Name );
        }
        else
        {
            this.Append( member.Name );
        }
    }

    public override void VisitParameter( IParameter declaration )
    {
        if ( this._format.IncludeParent )
        {
            this.Visit( declaration.DeclaringMember );
            this.Append( "@" );
        }

        this.Append( declaration.IsReturnParameter ? "<return>" : declaration.Name );
    }

    public override void VisitIndexer( IIndexer declaration )
    {
        if ( this._format.IncludeParent )
        {
            this.Visit( declaration.DeclaringType );
            this.Append( "." );
        }

        this.Append( "this[" );
        this.VisitParameterList( declaration.Parameters );
        this.Append( "]" );
    }

    public override void VisitEvent( IEvent declaration )
    {
        if ( this._format.IncludeParent )
        {
            this.Visit( declaration.DeclaringType );
            this.Append( "." );
        }

        this.PrintDeclarationName( declaration );
    }

    public override void VisitField( IField declaration )
    {
        if ( this._format.IncludeParent )
        {
            this.Visit( declaration.DeclaringType );
            this.Append( "." );
        }

        // Format backing fields as PropertyName.field. Different Roslyn versions use different renderings,
        // and it's easier for tests if we unify it.
        if ( declaration.IsImplicitlyDeclared && FormatterHelper.TryGetBackedPropertyName( declaration.Name, out var propertyName ) )
        {
            this.Append( propertyName );
            this.Append( ".field" );
        }
        else
        {
            this.Append( declaration.Name );
        }
    }

    public override void VisitProperty( IProperty declaration )
    {
        if ( this._format.IncludeParent )
        {
            this.Visit( declaration.DeclaringType );
            this.Append( "." );
        }

        this.PrintDeclarationName( declaration );
    }

    public override void VisitMethod( IMethod declaration )
    {
        if ( declaration.MethodKind == MethodKind.Lambda )
        {
            this.Append( "lambda expression" );

            return;
        }

        if ( declaration.DeclaringMember != null )
        {
            this.Visit( declaration.DeclaringMember );
        }
        else if ( this._format.IncludeParent && declaration.MethodKind != MethodKind.LocalFunction )
        {
            this.Visit( declaration.DeclaringType );
            this.Append( "." );
        }

        switch ( declaration.MethodKind )
        {
            case MethodKind.Finalizer:
                this.Append( "~" );
                this.Append( declaration.DeclaringType.Name );
                this.Append( "()" );

                break;

            case MethodKind.Operator when _operators.TryGetValue( declaration.OperatorKind, out var operatorName ):
                this.Append( operatorName );
                PrintParameters();

                break;

            default:
                if ( _methodKinds.TryGetValue( declaration.MethodKind, out var methodKind ) )
                {
                    this.Append( "." );
                    this.Append( methodKind );
                }
                else
                {
                    this.PrintDeclarationName( declaration );

                    if ( declaration.TypeArguments.Count > 0 )
                    {
                        this.Append( "<" );
                        this.VisitTypeArgumentList( declaration.TypeArguments );
                        this.Append( ">" );
                    }

                    PrintParameters();
                }

                break;
        }

        void PrintParameters()
        {
            this.Append( "(" );
            this.VisitParameterList( declaration.Parameters );
            this.Append( ")" );
        }
    }

    public override void VisitCompilation( ICompilation declaration )
    {
        this.Append( declaration.Identity.ToString()! );
    }

    protected override void VisitNamedType( INamedType namedType )
    {
        switch ( namedType )
        {
            case { SpecialType: not 0 } when _specialType.TryGetValue( namedType.SpecialType, out var specialType ):
                this.Append( specialType );

                break;

            case { Name: nameof(ValueTuple), ContainingNamespace.FullName: "System" }:
                this.Append( "(" );

                for ( var index = 0; index < namedType.TypeArguments.Count; index++ )
                {
                    if ( index > 0 )
                    {
                        this.Append( ", " );
                    }

                    var typeArgument = namedType.TypeArguments[index];
                    this.Visit( typeArgument );
                }

                this.Append( ")" );

                break;

            case { Name: nameof(Nullable), ContainingNamespace.FullName: "System", TypeParameters.Count: 1 }:
                this.Visit( namedType.TypeArguments[0] );

                // The trailing ? is appended lower in this method.

                break;

            default:
                if ( namedType.DeclaringType != null )
                {
                    this.VisitNamedType( namedType.DeclaringType );
                    this.Append( "." );
                }

                this.Append( namedType.Name );

                if ( namedType.TypeArguments.Count > 0 )
                {
                    this.Append( "<" );
                    this.VisitTypeArgumentList( namedType.TypeArguments );
                    this.Append( ">" );
                }

                break;
        }

        if ( namedType.IsNullable == true )
        {
            this.Append( "?" );
        }
    }

    protected override void VisitTypeParameter( ITypeParameter typeParameter )
    {
        using ( StackOverflowHelper.Detect() )
        {
            if ( this._genericContext == null || this._genericContext.IsEmptyOrIdentity )
            {
                if ( this.RecursionDepth > 1 )
                {
                    // Make sure we are not recurring.
                    this.Append( typeParameter.Name );
                }
                else
                {
                    this.Visit( typeParameter.ContainingDeclaration! );
                    this.Append( "/" );
                    this.Append( typeParameter.Name );
                }
            }
            else
            {
                this.Visit( this._genericContext.Map( typeParameter ) );
            }
        }
    }

    protected override void VisitArrayType( IArrayType arrayType )
    {
        static IType GetNonArrayElementType( IType type )
            => type switch
            {
                IArrayType a => GetNonArrayElementType( a.ElementType ),
                _ => type
            };

        var nonArrayElementType = GetNonArrayElementType( arrayType.ElementType );

        this.Visit( nonArrayElementType );
        AppendArrayTypeRecursive( arrayType );

        void AppendArrayTypeRecursive( IArrayType t )
        {
            if ( t.Rank == 1 )
            {
                this.Append( "[]" );
            }
            else
            {
                this.Append( "[" );

                for ( var i = 1; i < t.Rank; i++ )
                {
                    this.Append( "," );
                }

                this.Append( "]" );
            }

            if ( t.IsNullable == true )
            {
                this.Append( "?" );
            }

            if ( t.ElementType is IArrayType a )
            {
                AppendArrayTypeRecursive( a );
            }
        }
    }

    protected override void VisitDynamicType( IDynamicType dynamicType )
    {
        this.Append( "dynamic" );

        if ( dynamicType.IsNullable == true )
        {
            this.Append( "?" );
        }
    }

    protected override void VisitPointerType( IPointerType pointerType )
    {
        this.Visit( pointerType.PointedAtType );
        this.Append( "*" );
    }

    protected override void VisitFunctionPointerType( IFunctionPointerType functionPointerType ) => this.Append( "<function pointer>" );

    protected override void VisitNull() => this.Append( "<uninitialized>" );
}