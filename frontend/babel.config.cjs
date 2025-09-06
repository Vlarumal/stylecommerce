module.exports = {
  presets: [
    ['@babel/preset-env', { targets: { node: 'current' } }],
    ['@babel/preset-typescript', { allowNamespaces: true }],
    ['@babel/preset-react', { runtime: 'automatic' }],
  ],
  plugins: [
    ['babel-plugin-react-compiler', { target: '18' }],
    ['babel-plugin-transform-import-meta', {
      replacements: {
        env: 'process.env',
      },
    }],
  ],
};
