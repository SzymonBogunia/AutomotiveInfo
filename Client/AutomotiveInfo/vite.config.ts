import { defineConfig } from 'vite';

// One Vite package for ALL of this site's backoffice extensions, following the official guide
// (Extend your project > Backoffice extensions > Development flow > Vite package setup).
//
// The folder is a PACKAGE, not a feature: Umbraco scans App_Plugins two levels deep for
// umbraco-package.json, and a single manifest may declare any number of extensions. So a new
// feature means a folder under src/, a key in `entry` below, and an entry in the manifest —
// never a second npm project.
//
// App_Plugins/AutomotiveInfo is 100% build output: the bundles plus everything copied from
// public/ (umbraco-package.json, lang/*.js). Nothing there is hand-edited, which is why
// emptyOutDir is safe and why the .csproj needs no entries for it.
export default defineConfig({
    build: {
        lib: {
            // Object form = one bundle per feature, named after the key. The backoffice then
            // loads only what an extension actually needs, instead of one growing bundle.
            entry: {
                'editorial-stats': 'src/editorial-stats/editorial-stats.element.ts',
            },
            formats: ['es'], // required for multi-entry lib builds; cjs/umd support only one
            // Without this, multi-entry es builds emit `<name>.mjs` and the manifest's
            // `element` path 404s. Pin it to .js.
            fileName: (_format, entryName) => `${entryName}.js`,
        },
        outDir: '../../App_Plugins/AutomotiveInfo',
        emptyOutDir: true,
        sourcemap: true,
        rollupOptions: {
            // Umbraco serves its own packages via an import map, so never bundle them.
            external: [/^@umbraco/],
        },
    },
    base: '/App_Plugins/AutomotiveInfo/',
});
