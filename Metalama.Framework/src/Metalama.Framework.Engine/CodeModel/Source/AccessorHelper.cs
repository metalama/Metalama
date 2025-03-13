// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Engine.CodeModel.Source
{
    internal static class AccessorHelper
    {
        public static IMethod? GetAccessorImpl( this IEvent @event, MethodKind kind )
            => kind switch
            {
                MethodKind.EventAdd => @event.AddMethod,
                MethodKind.EventRaise => @event.RaiseMethod,
                MethodKind.EventRemove => @event.RemoveMethod,
                _ => null
            };

        public static IMethod? GetAccessorImpl( this IFieldOrPropertyOrIndexer fieldOrPropertyOrIndexer, MethodKind kind )
            => kind switch
            {
                MethodKind.PropertyGet => fieldOrPropertyOrIndexer.GetMethod,
                MethodKind.PropertySet => fieldOrPropertyOrIndexer.SetMethod,
                _ => null
            };
    }
}