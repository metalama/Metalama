// Bundles the trimmed Monaco editor into ../editor: editor.js (exposes `self.monaco`), editor.css, editor.worker.js and the
// codicon font. The output is committed and embedded in metalama-mcp, so the build of the tool needs no Node.js.
import * as esbuild from 'esbuild';
import { readFileSync, rmSync, writeFileSync } from 'node:fs';

const out = '../editor';
rmSync( out, { recursive: true, force: true } );

const common = {
    bundle: true,
    minify: true,
    legalComments: 'none',
    target: 'es2022',
    logLevel: 'info',
    // Assets keep a stable name, so that the server can serve a fixed list of files.
    assetNames: '[name]',
    loader: { '.ttf': 'file' }
};

await esbuild.build( { ...common, entryPoints: { editor: 'entry.js' }, outdir: out, format: 'iife' } );
await esbuild.build( {
    ...common,
    entryPoints: { 'editor.worker': 'monaco-editor/editor/editor.worker.start' },
    outdir: out,
    format: 'iife'
} );

const pkg = JSON.parse( readFileSync( 'node_modules/monaco-editor/package.json', 'utf8' ) );
writeFileSync( `${out}/VERSION`, `monaco-editor ${pkg.version}\n` );
writeFileSync( `${out}/LICENSE`, readFileSync( 'node_modules/monaco-editor/LICENSE', 'utf8' ) );
