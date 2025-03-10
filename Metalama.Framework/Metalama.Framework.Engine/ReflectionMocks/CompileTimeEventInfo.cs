// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.SyntaxSerialization;
using System;
using System.Reflection;

namespace Metalama.Framework.Engine.ReflectionMocks
{
    internal sealed class CompileTimeEventInfo : EventInfo, ICompileTimeReflectionObject<IEvent>
    {
        public IRef<IEvent> Target { get; }

        public CompileTimeEventInfo( IEvent @event )
        {
            this.Target = @event.ToRef();
        }

        public static EventInfo Create( IEvent @event )
        {
            return new CompileTimeEventInfo( @event );
        }

        private static Exception CreateNotSupportedException() => CompileTimeMocksHelper.CreateNotSupportedException( "EventInfo" );

        public override object[] GetCustomAttributes( bool inherit ) => throw CreateNotSupportedException();

        public override object[] GetCustomAttributes( Type attributeType, bool inherit ) => throw CreateNotSupportedException();

        public override bool IsDefined( Type attributeType, bool inherit ) => throw CreateNotSupportedException();

        public override Type DeclaringType => throw CreateNotSupportedException();

        public override string Name => throw CreateNotSupportedException();

        public override Type ReflectedType => throw CreateNotSupportedException();

        public override MethodInfo GetAddMethod( bool nonPublic ) => throw CreateNotSupportedException();

        public override MethodInfo GetRaiseMethod( bool nonPublic ) => throw CreateNotSupportedException();

        public override MethodInfo GetRemoveMethod( bool nonPublic ) => throw CreateNotSupportedException();

        public override EventAttributes Attributes => throw CreateNotSupportedException();

        public bool IsAssignable => false;

        public IType Type => TypeFactory.GetType( typeof(EventInfo) );

        public Type ReflectionType => typeof(EventInfo);

        public RefKind RefKind => RefKind.None;

        public ref object? Value => ref RefHelper.Wrap( this );

        public TypedExpressionSyntax ToTypedExpressionSyntax( ISyntaxGenerationContext syntaxGenerationContext, IType? targetType = null )
            => CompileTimeMocksHelper.ToTypedExpressionSyntax( this, CompileTimeEventInfoSerializer.SerializeEvent, syntaxGenerationContext );
    }
}