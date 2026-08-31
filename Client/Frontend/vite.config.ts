import { defineConfig } from 'vite';
import tailwindcss from '@tailwindcss/vite';

// Sibling package to Client/AutomotiveInfo and Client/EditorialStatsDashboard,
// same "one npm project per concern" pattern. This one owns the public site's
// stylesheet only — no JS bundle, just Tailwind -> wwwroot/dist/site.css.
export default defineConfig({
    plugins: [tailwindcss()],
    build: {
        outDir: '../../wwwroot/dist', // Client/Frontend -> Client -> project root -> wwwroot/dist
        emptyOutDir: true,
        cssMinify: true,
        rollupOptions: {
            input: 'src/site.css',
            output: { assetFileNames: 'site.css' },
        },
    },
});