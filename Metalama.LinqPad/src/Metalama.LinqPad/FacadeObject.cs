// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using LINQPad;
using System;
using System.Collections.Generic;

namespace Metalama.LinqPad
{
    /// <summary>
    /// A facade object for non-trivial object. It exposes only the public properties and use <see cref="FacadePropertyFormatter"/>
    /// to format property values.
    /// </summary>
    internal sealed class FacadeObject : ICustomMemberProvider
    {
        private readonly FacadeType _type;
        private readonly object _instance;

        internal FacadeObject( FacadeType facadeType, object instance )
        {
            this._type = facadeType;
            this._instance = instance;
        }

        public IEnumerable<string> GetNames() => this._type.PropertyNames;

        public IEnumerable<Type> GetTypes() => this._type.PropertyTypes;

        public IEnumerable<object?> GetValues()
        {
            foreach ( var property in this._type.Properties )
            {
                object? value;

                try
                {
                    if ( property.IsLazy )
                    {
                        value = FacadePropertyFormatter.FormatLazyPropertyValue( this._instance, property.Type, property.GetFunc );
                    }
                    else
                    {
                        var rawValue = property.GetFunc( this._instance );
                        value = FacadePropertyFormatter.FormatPropertyValue( rawValue );
                    }
                }
                catch ( Exception e )
                {
                    value = FacadePropertyFormatter.FormatException( e );
                }

                yield return value;
            }
        }
    }
}