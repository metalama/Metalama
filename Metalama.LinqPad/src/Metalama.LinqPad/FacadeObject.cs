// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using LINQPad;
using Metalama.Framework.Code;
using Metalama.Framework.Workspaces.Navigation.Facade;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.LinqPad
{
    /// <summary>
    /// A facade object for non-trivial object. It exposes only the public properties and use <see cref="FacadePropertyFormatter"/>
    /// to format property values. Declarations get an additional <c>Permalink</c> property.
    /// </summary>
    internal sealed class FacadeObject : ICustomMemberProvider
    {
        private const string _permalinkPropertyName = "Permalink";

        private readonly FacadeObjectFactory _factory;
        private readonly FacadeType _type;
        private readonly object _instance;

        internal FacadeObject( FacadeObjectFactory factory, FacadeType facadeType, object instance )
        {
            this._factory = factory;
            this._type = facadeType;
            this._instance = instance;
        }

        private bool HasPermalink => this._instance is IDeclaration;

        public IEnumerable<string> GetNames() => this.HasPermalink ? this._type.PropertyNames.Append( _permalinkPropertyName ) : this._type.PropertyNames;

        public IEnumerable<Type> GetTypes() => this.HasPermalink ? this._type.PropertyTypes.Append( typeof(Permalink) ) : this._type.PropertyTypes;

        public IEnumerable<object?> GetValues()
        {
            foreach ( var property in this._type.Properties )
            {
                object? value;

                try
                {
                    var rawValue = property.GetFunc( this._instance );
                    value = FacadePropertyFormatter.FormatPropertyValue( rawValue );
                }
                catch ( Exception e )
                {
                    value = FacadePropertyFormatter.FormatException( e );
                }

                yield return value;
            }

            if ( this._instance is IDeclaration declaration )
            {
                var permalink = new Permalink( this._factory.GetGetCompilationInfo( declaration ), declaration );

                yield return FacadePropertyFormatter.FormatPropertyValue( permalink );
            }
        }
    }
}
