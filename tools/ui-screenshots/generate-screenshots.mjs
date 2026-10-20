// Generates docs/generated/ui-screenshots.md and its images from the running SPA so the UI documentation always matches the current build.
import { existsSync } from "node:fs";
import { mkdir, readFile, rm, writeFile } from "node:fs/promises";
import path from "node:path";
import process from "node:process";
import { chromium } from "playwright";

const baseUrl = process.env.UI_BASE_URL ?? "http://localhost:4200";
const templatePath = new URL("ui-screenshots.template.md", import.meta.url);
const imageFolder = "ui";

// MacBook Air class laptop and iPhone 15 class phone. The phone renders at 2x so text stays sharp in the docs.
const viewports = {
  laptop: {
    label: "Laptop",
    width: 1440,
    height: 900,
    deviceScaleFactor: 1,
    isMobile: false,
  },
  mobile: {
    label: "Mobile",
    width: 390,
    height: 844,
    deviceScaleFactor: 2,
    isMobile: true,
  },
};

// Users are the seeded demo identities stored by the SPA's role switcher. Claims are seeded references chosen to show each state.
const screens = [
  {
    id: "my-claims",
    title: "My claims",
    role: "Claimant",
    user: "alex",
    path: "/my-claims",
    summary: "A claimant's claims with their status and newest first.",
  },
  {
    id: "submit-claim",
    title: "Submit a claim",
    role: "Claimant",
    user: "alex",
    path: "/my-claims/new",
    summary: "The three-step submission form.",
  },
  {
    id: "claim-awaiting-information",
    title: "Claim awaiting information",
    role: "Claimant",
    user: "alex",
    path: "/my-claims/CLM-DEMO-0014",
    summary: "An open information request the claimant can answer.",
  },
  {
    id: "claim-settled",
    title: "Settled claim",
    role: "Claimant",
    user: "alex",
    path: "/my-claims/CLM-DEMO-0018",
    summary: "A final decision with the settlement amount and history.",
  },
  {
    id: "dashboard",
    title: "Dashboard",
    role: "Claims officer",
    user: "sam",
    path: "/dashboard",
    summary: "Team claim counts, workload, and outstanding liability.",
  },
  {
    id: "work-queue",
    title: "Work queue",
    role: "Claims officer",
    user: "sam",
    path: "/queue",
    summary: "Open claims with server-side filters, sorting, and paging.",
  },
  {
    id: "claim-review",
    title: "Claim review",
    role: "Claims officer",
    user: "sam",
    path: "/queue/CLM-DEMO-0007",
    summary:
      "An owned claim with estimate, information request, and decision actions.",
  },
];

if (process.argv.length > 3) {
  console.error(
    "Usage: npm --prefix tools/ui-screenshots run generate -- [output-path]",
  );
  process.exitCode = 1;
} else {
  await generateScreenshots(process.argv[2]);
}

async function generateScreenshots(requestedOutputPath) {
  const repositoryRoot = findRepositoryRoot();
  const outputPath = path.resolve(
    requestedOutputPath ??
      path.join(repositoryRoot, "docs", "generated", "ui-screenshots.md"),
  );
  const imageDirectory = path.join(path.dirname(outputPath), imageFolder);
  await assertAppIsRunning();

  // Start from an empty folder so screens removed from the list do not leave stale images behind.
  await rm(imageDirectory, { recursive: true, force: true });
  await mkdir(imageDirectory, { recursive: true });

  const browser = await chromium.launch();
  try {
    for (const [name, viewport] of Object.entries(viewports)) {
      const context = await browser.newContext({
        viewport: { width: viewport.width, height: viewport.height },
        deviceScaleFactor: viewport.deviceScaleFactor,
        isMobile: viewport.isMobile,
        hasTouch: viewport.isMobile,
        reducedMotion: "reduce",
      });
      const page = await context.newPage();
      for (const screen of screens) {
        await capture(
          page,
          screen,
          viewport,
          path.join(imageDirectory, `${screen.id}-${name}.png`),
        );
        console.log(`Captured ${screen.id} (${name})`);
      }
      await context.close();
    }
  } finally {
    await browser.close();
  }

  const template = await readFile(templatePath, "utf8");
  for (const placeholder of ["{{screens}}", "{{laptop}}", "{{mobile}}"]) {
    if (!template.includes(placeholder))
      throw new Error(`The screenshot template must contain ${placeholder}.`);
  }
  // Replacer functions keep any "$" sequences in the generated Markdown literal.
  const document = template
    .replace("{{laptop}}", () => dimensions(viewports.laptop))
    .replace("{{mobile}}", () => dimensions(viewports.mobile))
    .replace("{{screens}}", () => renderScreens());
  await writeFile(outputPath, document, "utf8");
  console.log(`Generated ${outputPath}`);
}

async function assertAppIsRunning() {
  try {
    const response = await fetch(baseUrl);
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
  } catch (error) {
    throw new Error(
      `The SPA is not reachable at ${baseUrl} (${error.message}). Start the API and the SPA first, or set UI_BASE_URL.`,
      { cause: error },
    );
  }
}

async function capture(page, screen, viewport, filePath) {
  // The role switcher keeps the selected demo user in localStorage, so set it before loading the screen.
  await page.goto(baseUrl);
  await page.evaluate(
    (user) => localStorage.setItem("claims-user", user),
    screen.user,
  );
  await page.setViewportSize({
    width: viewport.width,
    height: viewport.height,
  });
  await page.goto(new URL(screen.path, baseUrl).href);
  await page.waitForLoadState("networkidle");
  await page.locator(".loading-state").waitFor({ state: "detached" });

  // Grow the viewport to the full page height instead of using a full-page capture, which would leave fixed elements such as the mobile bottom navigation in the middle of the image.
  const height = await page.evaluate(
    // eslint-disable-next-line no-undef -- runs inside the browser page, where document exists
    () => document.documentElement.scrollHeight,
  );
  await page.setViewportSize({
    width: viewport.width,
    height: Math.max(height, viewport.height),
  });
  await page.screenshot({
    path: filePath,
    animations: "disabled",
    caret: "hide",
  });
}

function renderScreens() {
  const sections = [];
  let currentRole = null;
  for (const screen of screens) {
    if (screen.role !== currentRole) {
      currentRole = screen.role;
      sections.push(`## ${currentRole}`);
    }
    sections.push(
      [
        `### ${screen.title}`,
        "",
        `${screen.summary} \`${screen.path}\``,
        "",
        "<table>",
        `  <tr><th>${sizeLabel(viewports.laptop)}</th><th>${sizeLabel(viewports.mobile)}</th></tr>`,
        "  <tr>",
        `    <td valign="top"><img src="${imageFolder}/${screen.id}-laptop.png" width="640" alt="${screen.title} on a laptop"></td>`,
        `    <td valign="top"><img src="${imageFolder}/${screen.id}-mobile.png" width="240" alt="${screen.title} on a phone"></td>`,
        "  </tr>",
        "</table>",
      ].join("\n"),
    );
  }
  return sections.join("\n\n");
}

function dimensions(viewport) {
  return `${viewport.width} px`;
}

function sizeLabel(viewport) {
  return `${viewport.label} (${dimensions(viewport)} wide)`;
}

function findRepositoryRoot() {
  let directory = path.dirname(new URL(import.meta.url).pathname);
  while (!existsSync(path.join(directory, "Claims.slnx"))) {
    const parent = path.dirname(directory);
    if (parent === directory)
      throw new Error("Could not find the repository root (Claims.slnx).");
    directory = parent;
  }
  return directory;
}
