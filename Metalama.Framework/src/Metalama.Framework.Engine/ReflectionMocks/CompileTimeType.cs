// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System;
using System.Globalization;
using System.Reflection;
using RefKind = Metalama.Framework.Code.RefKind;

namespace Metalama.Framework.Engine.ReflectionMocks
{
    /// <summary>
    /// The base class of the reflection mocks that stand for a type that cannot be represented by a real, loadable
    /// <see cref="Type"/> — typically a run-time type of the compiled project, which is never loaded at compile time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Instances are structural: each kind of type has its own derived class, which answers the questions specific to
    /// that kind (<see cref="CompileTimeNamedType"/>, <see cref="CompileTimeArrayType"/>, <see cref="CompileTimePointerType"/>,
    /// <see cref="CompileTimeGenericParameterType"/>). This is what allows a mock to be serialized without a compilation:
    /// the writer can interrogate its shape instead of resolving it back to an <see cref="ITypeSymbol"/>.
    /// </para>
    /// <para>
    /// Instances must always be obtained from <see cref="CompileTimeTypeFactory"/>, which caches
    /// them by <see cref="SerializableTypeId"/> at every level of the hierarchy. Constructors are therefore not public:
    /// two mocks for the same type must be the same instance.
    /// </para>
    /// </remarks>
    internal abstract class CompileTimeType : Type, ICompileTimeReflectionObject<IType>, ICompileTimeType
    {
        /// <summary>
        /// Gets the id of the type this instance stands for. This is the whole of its identity: a mock holds an id and
        /// some metadata, never a reference that would have to be resolved against a compilation.
        /// </summary>
        [Memo]
        public SerializableTypeId TypeId => this._typeId ?? this.GetSerializableTypeId();

        // Materialized on demand for the callers that do have a compilation and want to resolve the type in it. The ref
        // is durable, i.e. it holds the id and nothing else.
        IRef<IType> ICompileTimeReflectionObject<IType>.Target => this.ToRef();

        internal IDurableRef<IType> ToRef() => DurableRefFactory.FromTypeId<IType>( this.TypeId );

        private readonly SerializableTypeId? _typeId;

        private protected CompileTimeType( SerializableTypeId? typeId )
        {
            this._typeId = typeId;
        }

        protected Exception CreateNotSupportedException() => CompileTimeMocksHelper.CreateNotSupportedException( this.TypeId.Id );

        // Namespace, Name, FullName and ToString are not stored: each kind computes them from its own structural fields
        // (an array from its element and rank, a named type from its metadata name, namespace, declaring type and
        // generic arguments, and so on), so that there is a single source of truth for the shape of the type.
        public abstract override string? Namespace { get; }

        public abstract override string Name { get; }

        public abstract override string FullName { get; }

        /// <summary>
        /// Gets the name of the <em>run-time</em> assembly that declares this type, or <c>null</c> if the type has no
        /// declaring assembly of its own (an array or a pointer, whose assembly is that of its element type).
        /// </summary>
        /// <remarks>
        /// <see cref="Assembly"/> cannot be implemented, because there is no loadable assembly to return. The name is
        /// exposed separately so that serialization can name the type without resolving it through a compilation.
        /// </remarks>
        internal abstract string? AssemblyName { get; }

        // The default implementation of GetTypeCodeImpl reads UnderlyingSystemType, which we cannot provide. None of
        // the types represented by a mock is a primitive: a primitive is always loadable, and so is never mocked.
        protected override TypeCode GetTypeCodeImpl() => TypeCode.Object;

        // Answered by the derived classes. The defaults hold for every kind that does not override them.
        protected override bool IsArrayImpl() => false;

        protected override bool IsPointerImpl() => false;

        protected override bool HasElementTypeImpl() => false;

        protected override bool IsByRefImpl() => false;

        protected override bool IsPrimitiveImpl() => false;

        protected override bool IsCOMObjectImpl() => false;

        protected override bool IsValueTypeImpl() => false;

        // Must be answered by every kind, not just the named one: the default implementation is IsSubclassOf(typeof(Enum)),
        // which reads BaseType, and GetIntrinsicType asks IsEnum of *every* type before anything else.
        public override bool IsEnum => false;

        public override bool IsGenericType => false;

        // Type.IsConstructedGenericType and ContainsGenericParameters are NOT implemented on the base (they throw
        // NotImplementedException); only RuntimeType implements them. A mock matches RuntimeType. IsConstructedGenericType
        // is a pure function of the two properties above, so it is correct at this level; ContainsGenericParameters
        // defaults to false and is overridden by the kinds that can carry a type parameter.
        public override bool IsConstructedGenericType => this.IsGenericType && !this.IsGenericTypeDefinition;

        public override bool ContainsGenericParameters => false;

        public override Type GetElementType() => throw this.CreateNotSupportedException();

        // Type.GetGenericArguments() is NOT empty by default: the base implementation throws NotSupportedException, and
        // only RuntimeType overrides it to return an empty array for a non-generic type. A mock must match RuntimeType,
        // so the non-generic default is empty here, and CompileTimeNamedType overrides it for an actual generic type.
        public override Type[] GetGenericArguments() => Type.EmptyTypes;

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

        // This mock *is* the type it stands for; there is no other Type to unwrap to. It must not throw: the BCL reads
        // UnderlyingSystemType from operations that a mock legitimately takes part in, notably RuntimeType.IsAssignableFrom,
        // which GetIntrinsicType calls on every type (`typeof(Type).IsAssignableFrom(type)`).
        public override Type UnderlyingSystemType => this;

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

        public override Type GetNestedType( string name, BindingFlags bindingAttr ) => throw this.CreateNotSupportedException();

        public override Type[] GetNestedTypes( BindingFlags bindingAttr ) => throw this.CreateNotSupportedException();

        public override Type GetInterface( string name, bool ignoreCase ) => throw this.CreateNotSupportedException();

        public override Type[] GetInterfaces() => throw this.CreateNotSupportedException();

        public abstract override string ToString();

        bool IExpression.IsAssignable => false;

        IType IHasType.Type => TypeFactory.GetType( typeof(Type) );

        Type ICompileTimeReflectionObject<IType>.ReflectionType => typeof(Type);

        RefKind IHasType.RefKind => RefKind.None;

        ref object? IExpression.Value => ref RefHelper.Wrap( this );

        public TypedExpressionSyntax ToTypedExpressionSyntax( ISyntaxGenerationContext syntaxGenerationContext, IType? targetType = null )
        {
            var compilation = ((SyntaxSerializationContext) syntaxGenerationContext).CompilationModel;

            return CompileTimeMocksHelper.ToTypedExpressionSyntax(
                this.ToRef()
                    .GetSymbol( compilation.RoslynCompilation )
                    .AssertCast<ITypeSymbol>()
                    .AssertSymbolNullNotImplemented( UnsupportedFeatures.IntroducedTypeSerialization ),
                typeof(Type),
                TypeSerializationHelper.SerializeTypeSymbolRecursive,
                syntaxGenerationContext );
        }

        public override bool Equals( Type? o ) => o is CompileTimeType compileTimeType && this.TypeId.Equals( compileTimeType.TypeId );

        public override int GetHashCode() => this.TypeId.GetHashCode();

        public override Type MakeArrayType() => this.MakeArrayType( 1 );

        public override Type MakeArrayType( int rank ) => new CompileTimeArrayType( null, this, rank );

        public override Type MakePointerType() => new CompileTimePointerType( null, this );
    }
}