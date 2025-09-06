// Custom transformer to handle import.meta.env in Jest
// This transformer uses Babel to transform TypeScript/ESM syntax and replaces import.meta.env with process.env during testing

import babelJest from 'babel-jest';

export function process(src, filename, options) {
  let transformedSrc = src;

  transformedSrc = transformedSrc.replace(/import\.meta\.env\.([A-Za-z0-9_]+)/g, 'process.env.$1');

  transformedSrc = transformedSrc.replace(/import\.meta\.env/g, 'process.env');

  const babelTransformer = babelJest.createTransformer({
    presets: [
      ['@babel/preset-env', { targets: { node: 'current' } }],
      ['@babel/preset-typescript', { allowNamespaces: true }],
      ['@babel/preset-react', { runtime: 'automatic' }]
    ]
  });

  return babelTransformer.process(transformedSrc, filename, options);
}
export function getCacheKey() {
  return 'custom-transformer-v2';
}