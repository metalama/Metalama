// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.SyntaxSerialization;
using System;
using System.Globalization;
using System.Reflection;

namespace Metalama.Framework.Engine.ReflectionMocks
{
    internal sealed class CompileTimeFieldInfo : FieldInfo, ICompileTimeReflectionObject<IFieldOrProperty>
    {
        public IRef<IFieldOrProperty> Target { get; }

        private CompileTimeFieldInfo( IField field )
        {
            this.Target = field.ToRef();
        }

        private static Exception CreateNotSupportedException() => CompileTimeMocksHelper.CreateNotSupportedException( "FieldInfo" );

        public static FieldInfo Create( IField field ) => new CompileTimeFieldInfo( field );

        public override object[] GetCustomAttributes( bool inherit ) => throw CreateNotSupportedException();

        public override object[] GetCustomAttributes( Type attributeType, bool inherit ) => throw CreateNotSupportedException();

        public override bool IsDefined( Type attributeType, bool inherit ) => throw CreateNotSupportedException();

        public override Type DeclaringType => throw CreateNotSupportedException();

        public override string Name => throw CreateNotSupportedException();

        public override Type ReflectedType => throw CreateNotSupportedException();

        public override FieldAttributes Attributes => throw CreateNotSupportedException();

        public override RuntimeFieldHandle FieldHandle => throw CreateNotSupportedException();

        public override Type FieldType => throw CreateNotSupportedException();

        public override object GetValue( object? obj ) => throw CreateNotSupportedException();

        public override void SetValue( object? obj, object? value, BindingFlags invokeAttr, Binder? binder, CultureInfo? culture )
            => throw CreateNotSupportedException();

        public bool IsAssignable => false;

        public IType Type => TypeFactory.GetType( typeof(FieldInfo) );

        public Type ReflectionType => typeof(FieldInfo);

        public RefKind RefKind => RefKind.None;

        public ref object? Value => ref RefHelper.Wrap( this );

        public TypedExpressionSyntax ToTypedExpressionSyntax( ISyntaxGenerationContext syntaxGenerationContext, IType? targetType = null )
            => CompileTimeMocksHelper.ToTypedExpressionSyntax(
                this,
                ( fieldOrProperty, context ) => CompileTimeFieldInfoSerializer.SerializeField( (IField) fieldOrProperty, context ),
                syntaxGenerationContext );
    }
}