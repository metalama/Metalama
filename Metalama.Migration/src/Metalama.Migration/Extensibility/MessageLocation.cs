// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Diagnostics;
using PostSharp.Reflection;
using PostSharp.Reflection.MethodBody;
using System;
using System.Reflection;

namespace PostSharp.Extensibility
{
    /// <summary>
    /// In Metalama, the equivalent is <see cref="IDiagnosticLocation"/>.
    /// </summary>
    [PublicAPI]
    public class MessageLocation
    {
        public static readonly MessageLocation Unknown;

        public static MessageLocation Of( SymbolSequencePoint symbolSequencePoint )
        {
            throw new NotImplementedException();
        }

        public static MessageLocation Explicit( string file, int lineStart, int columnStart, int lineEnd, int columnEnd )
        {
            throw new NotImplementedException();
        }

        public static MessageLocation Explicit( string file, int line, int column )
        {
            throw new NotImplementedException();
        }

        public static MessageLocation Explicit( string file )
        {
            throw new NotImplementedException();
        }

        public static MessageLocation Of( object codeElement )
        {
            throw new NotImplementedException();
        }

        public static MessageLocation Of( MemberInfo member )
        {
            throw new NotImplementedException();
        }

        public static MessageLocation Of( ParameterInfo parameter )
        {
            throw new NotImplementedException();
        }

        public static MessageLocation Of( LocationInfo location )
        {
            throw new NotImplementedException();
        }

        public static MessageLocation Of( Assembly assembly )
        {
            throw new NotImplementedException();
        }

        public static MessageLocation Of( IExpression expression )
        {
            throw new NotImplementedException();
        }

        public object CodeElement { get; }

        public string File { get; }

        public int StartLine { get; }

        public int StartColumn { get; }

        public int EndLine { get; }

        public int EndColumn { get; }
    }
}