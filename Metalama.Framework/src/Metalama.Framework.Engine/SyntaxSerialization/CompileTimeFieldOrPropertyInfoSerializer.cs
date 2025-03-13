// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Source;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.RunTime;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Reflection;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.SyntaxSerialization;

internal sealed class CompileTimeFieldOrPropertyInfoSerializer : ObjectSerializer<CompileTimeFieldOrPropertyInfo, FieldOrPropertyInfo>
{
    public override ExpressionSyntax Serialize( CompileTimeFieldOrPropertyInfo obj, SyntaxSerializationContext serializationContext )
        => SerializeFieldOrProperty( obj.FieldOrPropertyOrIndexer, serializationContext );

    public static ExpressionSyntax SerializeFieldOrProperty( IFieldOrPropertyOrIndexer member, SyntaxSerializationContext serializationContext )
    {
        var fieldInfoOrPropertyInfo = member switch
        {
            IPropertyOrIndexer property => CompileTimePropertyInfoSerializer.SerializeProperty( property, serializationContext ),
            IField field => CompileTimeFieldInfoSerializer.SerializeField( field, serializationContext ),
            _ => throw new NotImplementedException()
        };

        return ObjectCreationExpression(
                serializationContext.GetTypeSyntax( typeof(FieldOrPropertyInfo) ),
                ArgumentList( SingletonSeparatedList( Argument( fieldInfoOrPropertyInfo ) ) ),
                null )
            .NormalizeWhitespaceIfNecessary( serializationContext.SyntaxGenerationContext );
    }

    public CompileTimeFieldOrPropertyInfoSerializer( SyntaxSerializationService service ) : base( service ) { }

    protected override ImmutableArray<Type> AdditionalSupportedTypes => ImmutableArray.Create( typeof(MemberInfo), typeof(SourceMethodBase) );
}