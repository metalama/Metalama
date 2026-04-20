// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Spectre.Console;
using System;
using System.IO;
using System.Text;

namespace Metalama.Backstage.Commands
{
    internal sealed class AnsiConsoleOutputWrapper : IAnsiConsoleOutput
    {
        private const int _fallbackWidth = 16 * 1024;
        private const int _fallbackHeight = 256;

        private readonly TextWriter _underlying;

        public AnsiConsoleOutputWrapper( TextWriter underlying )
        {
            this._underlying = underlying;

            try
            {
                this.Width = Console.WindowWidth;
                this.Height = Console.WindowHeight;
            }
            catch ( IOException )
            {
                // Ignored: will be handled by the fallback below.
            }

            // On Linux without a TTY (e.g., Docker containers, CI pipelines), Console.WindowWidth/Height
            // returns 0 instead of throwing. Spectre.Console silently swallows all output when width is 0.
            if ( this.Width <= 0 )
            {
                this.Width = _fallbackWidth;
            }

            if ( this.Height <= 0 )
            {
                this.Height = _fallbackHeight;
            }
        }

        void IAnsiConsoleOutput.SetEncoding( Encoding encoding ) { }

        TextWriter IAnsiConsoleOutput.Writer => this._underlying;

        public bool IsTerminal => true;

        public int Width { get; }

        public int Height { get; }
    }
}