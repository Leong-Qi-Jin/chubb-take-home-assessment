// @ts-check
import eslint from "@eslint/js";
import prettier from "eslint-config-prettier";
import { defineConfig } from "eslint/config";
import globals from "globals";

// Repository-level JavaScript, such as the Node.js tools. apps/web has its own Angular config and apps/api uses dotnet format.
export default defineConfig(
  {
    ignores: ["apps/**", "**/node_modules/**", "docs/generated/**"],
  },
  {
    files: ["**/*.{js,mjs,cjs}"],
    extends: [eslint.configs.recommended],
    languageOptions: { globals: globals.node },
    rules: {
      eqeqeq: "error",
    },
  },
  // Last, so formatting rules never conflict with Prettier.
  prettier,
);
