// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System;
using System.Globalization;
using System.Reflection;
using RefKind = Metalama.Framework.Code.RefKind;

namespace Metalama.Framework.Engine.ReflectionMocks
{
    internal sealed class CompileTimeType : Type, ICompileTimeReflectionObject<IType>, ICompileTimeType
    {
        // We store a reference-typed ISdkRef instead of the value-typed Ref because it is only being accessed
        // through ICompileTimeReflectionObject, so boxing cannot be avoided anyway. It is better in this case
        // to box only once.
        internal IRef<IType> Target { get; }

        IRef<IType> ICompileTimeReflectionObject<IType>.Target => this.Target;

        private Exception CreateNotSupportedException() => CompileTimeMocksHelper.CreateNotSupportedException( this.Target.ToString() ?? "Type" );

        private CompileTimeType( IRef<IType> targetRef, ITypeSymbol symbolForMetadata )
        {
            this.Namespace = symbolForMetadata.ContainingNamespace.GetFullName();
            this.Name = symbolForMetadata.GetReflectionName().AssertNotNull();
            this.FullName = symbolForMetadata.GetReflectionFullName().AssertNotNull();
            this._toStringName = symbolForMetadata.GetReflectionToStringName().AssertNotNull();

            this.Target = targetRef;
        }

        private CompileTimeType( IRef<IType> targetRef, CompileTimeTypeMetadata metadata )
        {
            this.Namespace = metadata.Namespace;
            this.Name = metadata.Name;
            this.FullName = metadata.FullName;
            this._toStringName = metadata.ToStringName;

            this.Target = targetRef;
        }

        internal static CompileTimeType CreateFromTypeId( SerializableTypeId typeId, ITypeSymbol symbolForMetadata )
            => new( DurableRefFactory.FromTypeId<IType>( typeId ), symbolForMetadata );

        internal static CompileTimeType CreateFromTypeId( SerializableTypeId typeId, CompileTimeTypeMetadata metadata )
            => new( DurableRefFactory.FromTypeId<IType>( typeId ), metadata );

        // For test only.
        internal static CompileTimeType Create( IType type ) => Create( type.GetSymbol().AssertSymbolNotNull(), type.GetRefFactory() );

        // For test only.
        private static CompileTimeType Create( ITypeSymbol typeSymbol, RefFactory refFactory ) => new( typeSymbol.ToRef( refFactory ), typeSymbol );

        public override string? Namespace { get; }

        public override string Name { get; }

        public override string FullName { get; }

        private readonly string _toStringName;

        public override object[] GetCustomAttributes( bool inherit ) => throw this.CreateNotSupportedException();

        public override object[] GetCustomAttributes( Type attributeType, bool inherit ) => throw this.CreateNotSupportedException();

        public override bool IsDefined( Type attributeType, bool inherit ) => throw this.CreateNotSupportedException();

        public override Module Module => throw this.CreateNotSupportedException();

        protected override TypeAttributes GetAttributeFlagsImpl() => throw this.CreateNotSupportedException();

        protected override ConstructorInfo GetConstructorImpl(
            BindingFlags bindingAttr,
            Binder? binder,
            CallingConventions callConvention,
            Type[] types,
            ParameterModifier[]? modifiers )
            => throw this.CreateNotSupportedException();

        public override ConstructorInfo[] GetConstructors( BindingFlags bindingAttr ) => throw this.CreateNotSupportedException();

        public override Type GetElementType() => throw this.CreateNotSupportedException();

        public override EventInfo GetEvent( string name, BindingFlags bindingAttr ) => throw this.CreateNotSupportedException();

        public override EventInfo[] GetEvents( BindingFlags bindingAttr ) => throw this.CreateNotSupportedException();

        public override FieldInfo GetField( string name, BindingFlags bindingAttr ) => throw this.CreateNotSupportedException();

        public override FieldInfo[] GetFields( BindingFlags bindingAttr ) => throw this.CreateNotSupportedException();

        public override MemberInfo[] GetMembers( BindingFlags bindingAttr ) => throw this.CreateNotSupportedException();

        protected override MethodInfo GetMethodImpl(
            string name,
            BindingFlags bindingAttr,
            Binder? binder,
            CallingConventions callConvention,
            Type[]? types,
            ParameterModifier[]? modifiers )
            => throw this.CreateNotSupportedException();

        public override MethodInfo[] GetMethods( BindingFlags bindingAttr ) => throw this.CreateNotSupportedException();

        public override PropertyInfo[] GetProperties( BindingFlags bindingAttr ) => throw this.CreateNotSupportedException();

        public override object InvokeMember(
            string name,
            BindingFlags invokeAttr,
            Binder? binder,
            object? target,
            object?[]? args,
            ParameterModifier[]? modifiers,
            CultureInfo? culture,
            string[]? namedParameters )
            => throw this.CreateNotSupportedException();

        public override Type UnderlyingSystemType => throw this.CreateNotSupportedException();

        public override bool IsGenericType => throw this.CreateNotSupportedException();

        protected override bool IsArrayImpl() => throw this.CreateNotSupportedException();

        protected override bool IsByRefImpl() => throw this.CreateNotSupportedException();

        protected override bool IsCOMObjectImpl() => throw this.CreateNotSupportedException();

        protected override bool IsPointerImpl() => throw this.CreateNotSupportedException();

        protected override bool IsPrimitiveImpl() => throw this.CreateNotSupportedException();

        public override Assembly Assembly => throw this.CreateNotSupportedException();

        public override string AssemblyQualifiedName => throw this.CreateNotSupportedException();

        public override Type BaseType => throw this.CreateNotSupportedException();

        public override Guid GUID => throw this.CreateNotSupportedException();

        protected override PropertyInfo GetPropertyImpl(
            string name,
            BindingFlags bindingAttr,
            Binder? binder,
            Type? returnType,
            Type[]? types,
            ParameterModifier[]? modifiers )
            => throw this.CreateNotSupportedException();

        protected override bool HasElementTypeImpl() => throw this.CreateNotSupportedException();

        public override Type GetNestedType( string name, BindingFlags bindingAttr ) => throw this.CreateNotSupportedException();

        public override Type[] GetNestedTypes( BindingFlags bindingAttr ) => throw this.CreateNotSupportedException();

        public override Type GetInterface( string name, bool ignoreCase ) => throw this.CreateNotSupportedException();

        public override Type[] GetInterfaces() => throw this.CreateNotSupportedException();

        public override string ToString() => this._toStringName;

        public bool IsAssignable => false;

        public IType Type => TypeFactory.GetType( typeof(Type) );

        public Type ReflectionType => typeof(Type);

        public RefKind RefKind => RefKind.None;

        public ref object? Value => ref RefHelper.Wrap( this );

        public TypedExpressionSyntax ToTypedExpressionSyntax( ISyntaxGenerationContext syntaxGenerationContext, IType? targetType = null )
        {
            var compilation = ((SyntaxSerializationContext) syntaxGenerationContext).CompilationModel;

            return CompileTimeMocksHelper.ToTypedExpressionSyntax(
                this.Target.GetSymbol( compilation.RoslynCompilation )
                    .AssertCast<ITypeSymbol>()
                    .AssertSymbolNullNotImplemented( UnsupportedFeatures.IntroducedTypeSerialization ),
                this.ReflectionType,
                TypeSerializationHelper.SerializeTypeSymbolRecursive,
                syntaxGenerationContext );
        }
    }
}