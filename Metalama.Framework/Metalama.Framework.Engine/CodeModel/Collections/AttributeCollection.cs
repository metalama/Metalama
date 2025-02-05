// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Collections
{
    internal sealed class AttributeCollection : DeclarationCollection<IAttribute, IRef<IAttribute>>, IAttributeCollection
    {
        public static AttributeCollection Empty { get; } = new();

        public AttributeCollection( IDeclaration declaration, IReadOnlyList<IRef<IAttribute>> sourceItems )
            : base( declaration, sourceItems ) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeCollection"/> class that contains no element.
        /// </summary>
        private AttributeCollection() { }

        IEnumerable<IAttribute> IAttributeCollection.OfAttributeType( IType type ) => this.OfAttributeType( type );

        IEnumerable<IAttribute> IAttributeCollection.OfAttributeType( IType type, ConversionKind conversionKind )
            => this.OfAttributeType( type, conversionKind );

        private IEnumerable<IAttribute> OfAttributeType( IType type, ConversionKind conversionKind = ConversionKind.Default )
            => this.GetItems(
                this.Source.Where(
                    a =>
                    {
                        var attributeType = ((AttributeRef) a).AttributeType.ToFullRef( type.GetRefFactory() ).ConstructedDeclaration;

                        return attributeType.IsConvertibleTo( type, conversionKind );
                    } ) );

        IEnumerable<IAttribute> IAttributeCollection.OfAttributeType( Type type ) => this.OfAttributeType( type );

        IEnumerable<IAttribute> IAttributeCollection.OfAttributeType( Type type, ConversionKind conversionKind )
            => this.OfAttributeType( type, conversionKind );

        public IEnumerable<IAttribute> OfAttributeType( Func<IType, bool> predicate )
            => this.GetItems( this.Source.Where( a => predicate( ((AttributeRef) a).AttributeType.GetTarget( this.Compilation ) ) ) );

        public IEnumerable<T> GetConstructedAttributesOfType<T>()
            where T : Attribute
            => this.OfAttributeType( typeof(T) ).Select( a => a.Construct<T>() );

        private IEnumerable<IAttribute> OfAttributeType( Type type, ConversionKind conversionKind = ConversionKind.Default )
        {
            if ( this.ContainingDeclaration == null )
            {
                // The collection is empty.
                return [];
            }

            return this.OfAttributeType(
                (INamedType) this.ContainingDeclaration!.GetCompilationModel().Factory.GetTypeByReflectionType( type ),
                conversionKind );
        }

        bool IAttributeCollection.Any( IType type ) => this.Any( type );

        bool IAttributeCollection.Any( IType type, ConversionKind conversionKind ) => this.Any( type, conversionKind );

        private bool Any( IType type, ConversionKind conversionKind = ConversionKind.Default )
            => this.Source.Any( a => ((AttributeRef) a).AttributeType.GetTarget( this.Compilation ).IsConvertibleTo( type, conversionKind ) );

        bool IAttributeCollection.Any( Type type ) => this.Any( type );

        bool IAttributeCollection.Any( Type type, ConversionKind conversionKind ) => this.Any( type, conversionKind );

        private bool Any( Type type, ConversionKind conversionKind = ConversionKind.Default )
        {
            if ( this.ContainingDeclaration == null )
            {
                // The collection is empty.
                return false;
            }

            return this.Any( (INamedType) this.ContainingDeclaration!.GetCompilationModel().Factory.GetTypeByReflectionType( type ), conversionKind );
        }
    }
}