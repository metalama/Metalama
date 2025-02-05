// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.SyntaxSerialization
{
    /// <summary>
    /// Serializes objects into Roslyn creation expressions that would create those objects. You can register additional serializers with an instance of this class
    /// to support additional types.
    /// </summary>
    internal sealed class SyntaxSerializationService : IProjectService
    {
        // Set of serializers indexed by the real implementation type they are able to handle (e.g. CompileTimeMethodInfo). 
        private readonly ConcurrentDictionary<Type, ObjectSerializer> _serializerByInputType = new();

        // Set of serializers indexed by the contract type they are able to handle. (e.g. MethodInfo). Used for compile-time validation.
        private readonly ConcurrentDictionary<Type, Type> _supportedContractTypes = new();
        private readonly ArraySerializer _arraySerializer;
        private readonly EnumSerializer _enumSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyntaxSerializationService"/> class.
        /// </summary>
        public SyntaxSerializationService()
        {
            // Arrays, enums
            this._arraySerializer = new ArraySerializer( this );
            this._enumSerializer = new EnumSerializer( this );

            // Primitive types
            this.RegisterSerializer( new CharSerializer( this ) );
            this.RegisterSerializer( new BoolSerializer( this ) );
            this.RegisterSerializer( new ByteSerializer( this ) );
            this.RegisterSerializer( new SByteSerializer( this ) );
            this.RegisterSerializer( new UShortSerializer( this ) );
            this.RegisterSerializer( new ShortSerializer( this ) );
            this.RegisterSerializer( new UIntSerializer( this ) );
            this.RegisterSerializer( new IntSerializer( this ) );
            this.RegisterSerializer( new ULongSerializer( this ) );
            this.RegisterSerializer( new LongSerializer( this ) );
            this.RegisterSerializer( new FloatSerializer( this ) );
            this.RegisterSerializer( new DoubleSerializer( this ) );
            this.RegisterSerializer( new DecimalSerializer( this ) );
            this.RegisterSerializer( new UIntPtrSerializer( this ) );
            this.RegisterSerializer( new IntPtrSerializer( this ) );

            // String
            this.RegisterSerializer( new StringSerializer( this ) );

            // Known simple system types
            this.RegisterSerializer( new DateTimeSerializer( this ) );
            this.RegisterSerializer( new GuidSerializer( this ) );
            this.RegisterSerializer( new TimeSpanSerializer( this ) );
            this.RegisterSerializer( new DateTimeOffsetSerializer( this ) );
            this.RegisterSerializer( new CultureInfoSerializer( this ) );

            // Collections
            this.RegisterSerializer( new ListSerializer( this ) );
            this.RegisterSerializer( new DictionarySerializer( this ) );

            // Reflection types
            this.TypeSerializer = new TypeSerializer( this );
            this.CompileTimeMethodInfoSerializer = new CompileTimeMethodInfoSerializer( this );
            this.CompileTimePropertyInfoSerializer = new CompileTimePropertyInfoSerializer( this );
            this.RegisterSerializer( this.TypeSerializer );
            this.RegisterSerializer( this.CompileTimeMethodInfoSerializer );
            this.RegisterSerializer( this.CompileTimePropertyInfoSerializer );
            this.RegisterSerializer( new CompileTimeConstructorInfoSerializer( this ) );
            this.RegisterSerializer( new CompileTimeEventInfoSerializer( this ) );
            this.RegisterSerializer( new CompileTimeParameterInfoSerializer( this ) );
            this.RegisterSerializer( new CompileTimeReturnParameterInfoSerializer( this ) );
            this.RegisterSerializer( new CompileTimeFieldOrPropertyInfoSerializer( this ) );
            this.RegisterSerializer( new CompileTimeFieldInfoSerializer( this ) );

            // Tuples.
            this.RegisterSerializer( new ValueTupleSerializer.Size1( this ) );
            this.RegisterSerializer( new ValueTupleSerializer.Size2( this ) );
            this.RegisterSerializer( new ValueTupleSerializer.Size3( this ) );
            this.RegisterSerializer( new ValueTupleSerializer.Size4( this ) );
            this.RegisterSerializer( new ValueTupleSerializer.Size5( this ) );
            this.RegisterSerializer( new ValueTupleSerializer.Size6( this ) );
            this.RegisterSerializer( new ValueTupleSerializer.Size7( this ) );
            this.RegisterSerializer( new ValueTupleSerializer.Size8( this ) );

            // Dynamic syntax
            this.RegisterSerializer( new ExpressionBuilderSerializer( this ) );
            this.RegisterSerializer( new ExpressionSerializer( this ) );
        }

        private TypeSerializer TypeSerializer { get; }

        private CompileTimeMethodInfoSerializer CompileTimeMethodInfoSerializer { get; }

        private CompileTimePropertyInfoSerializer CompileTimePropertyInfoSerializer { get; }

        /// <summary>
        /// Registers an additional serializer. See Remarks for generics.
        /// </summary>
        /// <remarks>
        /// For generic types, register the type without generic arguments, for example "List&lt;&gt;" rather than "List&lt;int&gt;". The serializer will handle
        /// lists of any element.
        /// </remarks>
        /// <param name="serializer">A new serializer that supports that type.</param>
        private void RegisterSerializer( ObjectSerializer serializer )
        {
            _ = this._serializerByInputType.TryAdd( serializer.InputType, serializer );

            foreach ( var inputType in serializer.AllSupportedTypes )
            {
                if ( inputType.IsPublic )
                {
                    this._supportedContractTypes.TryAdd( inputType, inputType );
                }
            }
        }

        /// <summary>
        /// Returns the set of types that can be serialized by <see cref="SyntaxSerializationService"/>. This result can be used to
        /// determine the possibility to serialize a type when the template is compiled. It may return false positives.
        /// </summary>
        public SerializableTypes GetSerializableTypes( CompilationContext compilationContext )
            => new(
                this._supportedContractTypes.Keys.Distinct()
                    .Select( compilationContext.ReflectionMapper.GetTypeSymbol )
                    .ToImmutableHashSet<ITypeSymbol>( compilationContext.SymbolComparer ) );

        private bool TryGetSerializer<T>( T obj, [NotNullWhen( true )] out ObjectSerializer? serializer )
        {
            switch ( obj )
            {
                case null:
                    throw new ArgumentNullException( nameof(obj) );

                case Enum:
                    serializer = this._enumSerializer;

                    return true;

                case Array:
                    serializer = this._arraySerializer;

                    return true;

                default:
                    return this.TryGetSerializer( obj.GetType(), typeof(T), out serializer );
            }
        }

        private bool TryGetSerializer( Type concreteType, Type contractType, [NotNullWhen( true )] out ObjectSerializer? serializer )
        {
            Type concreteTypeDeclaration = GetRealType( concreteType ), contractTypeDeclaration = GetRealType( contractType );

            if ( this._serializerByInputType.TryGetValue( concreteTypeDeclaration, out serializer )
                 && ValidateContractType( contractTypeDeclaration, serializer ) )
            {
                return true;
            }
            else if ( concreteTypeDeclaration.BaseType != null && this.TryGetSerializer(
                         concreteTypeDeclaration.BaseType,
                         contractTypeDeclaration,
                         out serializer ) )
            {
                return true;
            }
            else
            {
                List<ObjectSerializer>? serializers = null;

                foreach ( var interfaceImplementation in concreteTypeDeclaration.GetInterfaces() )
                {
                    if ( this.TryGetSerializer( interfaceImplementation, contractTypeDeclaration, out var interfaceSerializer ) )
                    {
                        serializers ??= new List<ObjectSerializer>();
                        serializers.Add( interfaceSerializer );
                    }
                }

                switch ( serializers?.Count )
                {
                    case 0:
                    case null:
                        break;

                    case 1:
                        serializer = serializers[0];

                        return true;

                    default:
                        serializer = serializers.OrderBy( s => s.Priority ).First();

                        return true;
                }
            }

            return false;
        }

        private static Type GetRealType( Type contractType )
        {
            Type contractTypeDeclaration;

            if ( contractType.IsGenericType )
            {
                if ( contractType.GetGenericTypeDefinition() == typeof(Nullable<>) )
                {
                    contractTypeDeclaration = contractType.GenericTypeArguments[0];
                }
                else
                {
                    contractTypeDeclaration = contractType.GetGenericTypeDefinition();
                }

                // TODO: Check that the output type arguments are compatible with the contract type arguments.
            }
            else
            {
                contractTypeDeclaration = contractType;
            }

            return contractTypeDeclaration;
        }

        private static bool ValidateContractType( Type contractType, ObjectSerializer serializer )
            => serializer.OutputType == null || contractType.IsAssignableFrom( serializer.OutputType );

        /// <summary>
        /// Serializes an object into a Roslyn expression that would create it. For example, serializes a list containing "4" and "8" into <c>new System.Collections.Generic.List&lt;System.Int32&gt;{4, 8}</c>.
        /// </summary>
        /// <param name="o">An object to serialize.</param>
        /// <param name="serializationContext"></param>
        /// <returns>An expression that would create the object.</returns>
        /// <exception cref="DiagnosticException">When the object cannot be serialized, for example if it's of an unsupported type.</exception>
        public ExpressionSyntax Serialize<T>( T? o, SyntaxSerializationContext serializationContext )
        {
            if ( !this.TrySerialize( o, serializationContext, out var expression ) )
            {
                throw SerializationDiagnosticDescriptors.UnsupportedSerialization.CreateException( o!.GetType() );
            }
            else
            {
                return expression;
            }
        }

        public bool TrySerialize<T>( T? o, SyntaxSerializationContext serializationContext, [NotNullWhen( true )] out ExpressionSyntax? expression )
        {
            if ( o == null )
            {
                expression = LiteralExpression( SyntaxKind.NullLiteralExpression );

                return true;
            }

            if ( !this.TryGetSerializer( o, out var serializer ) )
            {
                expression = null;

                return false;
            }

            using ( serializationContext.WithSerializeObject( o ) )
            {
                expression = serializer.Serialize( o, serializationContext );
            }

            return true;
        }
    }
}