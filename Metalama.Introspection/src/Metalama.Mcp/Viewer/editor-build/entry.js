// The editor API, its features and the C# grammar only: no TypeScript, JSON, CSS or HTML language service. The page gets
// completion, quick info, signature help and diagnostics from the worker of metalama-mcp instead.
import * as monaco from 'monaco-editor/editor/editor.api';
import 'monaco-editor/features/register.all';
import 'monaco-editor/languages/definitions/csharp/register';

self.monaco = monaco;
