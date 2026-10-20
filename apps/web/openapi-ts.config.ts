import { defineConfig } from '@hey-api/openapi-ts';

export default defineConfig({
  // The generation command relaxes TLS only for this hard-coded local endpoint because Node does not use the trusted ASP.NET development certificate.
  input: 'https://localhost:5001/openapi/v1.json',
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
