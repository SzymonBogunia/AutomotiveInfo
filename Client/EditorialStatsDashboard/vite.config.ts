import { defineConfig } from 'vite';

export default defineConfig({
    build: {
        lib: {
            entry: 'src/editorial-stats-dashboard.element.ts',
            formats: ['es'],
            fileName: () => 'editorial-stats-dashboard.js',
        },
        outDir: '../../App_Plugins/EditorialStatsDashboard/dist',
        emptyOutDir: true,
        rollupOptions: {
            external: [/^@umbraco-cms\//],
        },
    },
});