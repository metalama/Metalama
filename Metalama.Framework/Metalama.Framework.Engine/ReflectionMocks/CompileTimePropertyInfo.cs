// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.SyntaxSerialization;
using System;
using System.Globalization;
using System.Reflection;

namespace Metalama.Framework.Engine.ReflectionMocks
{
    internal sealed class CompileTimePropertyInfo : PropertyInfo, ICompileTimeReflectionObject<IPropertyOrIndexer>
    {
        public IRef<IPropertyOrIndexer> Target { get; }

        private CompileTimePropertyInfo( IPropertyOrIndexer property )
        {
            this.Target = property.ToRef();
        }

        private static Exception CreateNotSupportedException() => CompileTimeMocksHelper.CreateNotSupportedException( "PropertyInfo" );

        public static PropertyInfo Create( IPropertyOrIndexer property ) => new CompileTimePropertyInfo( property );

        public override object[] GetCustomAttributes( bool inherit ) => throw CreateNotSupportedException();

        public override object[] GetCustomAttributes( Type attributeType, bool inherit ) => throw CreateNotSupportedException();

        public override bool IsDefined( Type attributeType, bool inherit ) => throw CreateNotSupportedException();

        public override Type DeclaringType => throw CreateNotSupportedException();

        public override string Name => throw CreateNotSupportedException();

        public override Type ReflectedType => throw CreateNotSupportedException();

        public override MethodInfo[] GetAccessors( bool nonPublic ) => throw CreateNotSupportedException();

        public override MethodInfo GetGetMethod( bool nonPublic ) => throw CreateNotSupportedException();

        public override ParameterInfo[] GetIndexParameters() => throw CreateNotSupportedException();

        public override MethodInfo GetSetMethod( bool nonPublic ) => throw CreateNotSupportedException();

        public override object GetValue( object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture )
            => throw CreateNotSupportedException();

        public override void SetValue( object? obj, object? value, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture )
            => throw CreateNotSupportedException();

        public override PropertyAttributes Attributes => throw CreateNotSupportedException();

        public override bool CanRead => throw CreateNotSupportedException();

        public override bool CanWrite => throw CreateNotSupportedException();

        public override Type PropertyType => throw CreateNotSupportedException();

        public bool IsAssignable => false;

        public IType Type => TypeFactory.GetType( typeof(PropertyInfo) );

        public Type ReflectionType => typeof(PropertyInfo);

        public RefKind RefKind => RefKind.None;

        public ref object? Value => ref RefHelper.Wrap( this );

        public TypedExpressionSyntax ToTypedExpressionSyntax( ISyntaxGenerationContext syntaxGenerationContext, IType? targetType = null )
            => CompileTimeMocksHelper.ToTypedExpressionSyntax( this, CompileTimePropertyInfoSerializer.SerializeProperty, syntaxGenerationContext );
    }
}