import { defineConfig } from '@hey-api/openapi-ts';

export default defineConfig({
  input: 'http://localhost:5000/openapi/v1.json',
  output: {
    path: 'src/app/core/api/generated',
    clean: true,
  },
  plugins: [
    '@hey-api/client-angular',
    {
      name: '@hey-api/sdk',
      operations: 'flat',
    },
    '@hey-api/typescript',
  ],
});
