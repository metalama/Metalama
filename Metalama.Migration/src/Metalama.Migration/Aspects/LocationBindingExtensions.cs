// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace PostSharp.Aspects
{
    public static class LocationBindingExtensions
    {
        public static object GetValue( this ILocationBinding locationBinding, object instance, Arguments index )
        {
            throw new NotImplementedException();
        }

        public static T GetValue<T>( this ILocationBinding<T> locationBinding, object instance, Arguments index )
        {
            throw new NotImplementedException();
        }

        public static object GetValue( this ILocationBinding locationBinding, object instance )
        {
            throw new NotImplementedException();
        }

        public static T GetValue<T>( this ILocationBinding<T> locationBinding, object instance )
        {
            throw new NotImplementedException();
        }

        public static void SetValue( this ILocationBinding locationBinding, object instance, Arguments index, object value )
        {
            throw new NotImplementedException();
        }

        public static void SetValue<T>( this ILocationBinding<T> locationBinding, object instance, Arguments index, T value )
        {
            throw new NotImplementedException();
        }

        public static void SetValue<T>( this ILocationBinding<T> locationBinding, object instance, T value )
        {
            throw new NotImplementedException();
        }

        public static void SetValue( this ILocationBinding locationBinding, object instance, object value )
        {
            throw new NotImplementedException();
        }
    }
}