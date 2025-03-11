// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Diagnostics;
using PostSharp.Reflection;
using System;
using System.Reflection;

namespace PostSharp.Extensibility
{
    /// <summary>
    /// In Metalama, a message is represented by a <see cref="Metalama.Framework.Diagnostics.IDiagnostic"/>.
    /// To instantiate a diagnostic, you must first declare its <see cref="DiagnosticDefinition{T}"/> as a static field or property, then use
    /// <see cref="DiagnosticDefinition{T}.WithArguments"/>.
    /// </summary>
    /// <seealso href="@diagnostics"/>
    [PublicAPI]
    public sealed class Message
    {
        #region Fields

        public const int NotAvailable = 0;

        #endregion

        public Message(
            MessageLocation location,
            SeverityType severity,
            string messageId,
            string messageText,
            string helpLink,
            string source,
            Exception innerException )
        {
            throw new NotImplementedException();
        }

        public Message(
            SeverityType severity,
            string messageId,
            string messageText,
            string helpLink,
            string source,
            string locationFile,
            int locationLine,
            int locationColumn,
            Exception innerException )
        {
            throw new NotImplementedException();
        }

        public Message(
            SeverityType severity,
            string messageId,
            string messageText,
            string helpLink,
            string source,
            string locationFile,
            int locationStartLine,
            int locationStartColumn,
            int locationEndLine,
            int locationEndColumn,
            Exception innerException )
        {
            throw new NotImplementedException();
        }

        #region Properties

        public SeverityType Severity { get; internal set; }

        public string MessageId { get; }

        public MessageLocation Location { get; set; }

        public Exception InnerException { get; }

        public string Source { get; }

        public string MessageText { get; }

        public string HelpLink { get; }

        #endregion

        public static string GetExceptionStackMessage( Exception outerException )
        {
            throw new NotImplementedException();
        }

        public static void Write( Message message )
        {
            throw new NotImplementedException();
        }

        public static void Write( MessageLocation messageLocation, SeverityType severity, string errorCode, string message )
        {
            throw new NotImplementedException();
        }

        public static void Write( MessageLocation messageLocation, SeverityType severity, string errorCode, string format, params object[] arguments )
        {
            throw new NotImplementedException();
        }

        public static void Write( MemberInfo codeElement, SeverityType severity, string errorCode, string format, params object[] arguments )
        {
            throw new NotImplementedException();
        }

        public static void Write( Type type, SeverityType severity, string errorCode, string format, params object[] arguments )
        {
            throw new NotImplementedException();
        }

        public static void Write( ParameterInfo codeElement, SeverityType severity, string errorCode, string format, params object[] arguments )
        {
            throw new NotImplementedException();
        }

        public static void Write( Assembly codeElement, SeverityType severity, string errorCode, string format, params object[] arguments )
        {
            throw new NotImplementedException();
        }

        public static void Write( LocationInfo codeElement, SeverityType severity, string errorCode, string format, params object[] arguments )
        {
            throw new NotImplementedException();
        }
    }
}