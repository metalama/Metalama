// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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