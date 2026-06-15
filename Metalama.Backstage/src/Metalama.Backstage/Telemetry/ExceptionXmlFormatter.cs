// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections;
using System.Xml;
using JetBrains.Annotations;

namespace Metalama.Backstage.Telemetry
{
    // Warning: this file is linked to UserInterface solution. We need to serialize
    // exceptions from debugging server in the same way as ExceptionPackager does without
    // referencing PostSharp.Compiler.Settings.
    [PublicAPI]
    public static class ExceptionXmlFormatter
    {
        public static void WriteException( XmlWriter writer, Exception e ) => WriteException( writer, e, ExceptionSensitiveDataHelper.Instance );

        // Renders the exception with the given <paramref name="scrubber"/>. Pass ExceptionSensitiveDataHelper.Disabled
        // to render the full, unscrubbed local report shown next to the scrubbed upload payload on the review page. See #1674.
        internal static void WriteException( XmlWriter writer, Exception e, ExceptionSensitiveDataHelper scrubber )
        {
            writer.WriteElementString( "Type", scrubber.RemoveSensitiveData( e.GetType().FullName ) );

            // Exception.Message and Exception.Data are arbitrary, developer-populated content that frequently embeds
            // user input, file paths, connection strings or other secrets, so they are NEVER part of the uploaded
            // payload (#1680). They are included only in the full, unscrubbed local rendering shown for review (when the
            // scrubber is disabled), so the user can see exactly what is withheld from upload before sending. See #1674.
            if ( !scrubber.IsEnabled )
            {
                writer.WriteElementString( "Message", scrubber.RemoveSensitiveData( e.Message ) );
            }

            writer.WriteElementString( "Source", scrubber.RemoveSensitiveData( e.Source ) );

            if ( !scrubber.IsEnabled )
            {
                writer.WriteStartElement( "Data" );

                foreach ( DictionaryEntry? data in e.Data )
                {
                    writer.WriteStartElement( "Item" );

                    if ( data != null )
                    {
                        writer.WriteElementString( "Key", scrubber.RemoveSensitiveData( data.Value.Key.ToString() ) );

                        if ( data.Value.Value != null )
                        {
                            switch ( data.Value.Value )
                            {
                                case Array array:
                                    {
                                        writer.WriteStartElement( "Array" );

                                        for ( var i = 0; i < array.Length; i++ )
                                        {
                                            var value = array.GetValue( i );

                                            switch ( value )
                                            {
                                                case Exception exception:
                                                    writer.WriteStartElement( "Item" );
                                                    WriteException( writer, exception, scrubber );
                                                    writer.WriteEndElement();

                                                    break;

                                                case null:
                                                    writer.WriteElementString( "Item", "<null>" );

                                                    break;

                                                default:
                                                    writer.WriteElementString(
                                                        "Item",
                                                        scrubber.RemoveSensitiveData( value.ToString() ) );

                                                    break;
                                            }
                                        }

                                        writer.WriteEndElement();

                                        break;
                                    }

                                case Exception exception:
                                    writer.WriteStartElement( "Value" );
                                    WriteException( writer, exception, scrubber );
                                    writer.WriteEndElement();

                                    break;

                                default:
                                    writer.WriteElementString( "Value", scrubber.RemoveSensitiveData( data.Value.Value.ToString() ) );

                                    break;
                            }
                        }
                    }

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            writer.WriteElementString(
                "StackTrace",
                Environment.NewLine + scrubber.RemoveSensitiveData( e.StackTrace ) + Environment.NewLine );

            if ( e.InnerException != null )
            {
                writer.WriteStartElement( "InnerException" );
                WriteException( writer, e.InnerException, scrubber );
                writer.WriteEndElement();
            }

            if ( e is AggregateException aggregate )
            {
                writer.WriteStartElement( "InnerExceptions" );

                foreach ( var innerException in aggregate.InnerExceptions )
                {
                    writer.WriteStartElement( "Exception" );
                    WriteException( writer, innerException, scrubber );
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }
        }
    }
}
