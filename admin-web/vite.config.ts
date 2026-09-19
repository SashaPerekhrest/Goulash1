import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

function redirectAdminRoot() {
  return {
    name: "redirect-admin-root",
    configureServer(server) {
      server.middlewares.use((req, res, next) => {
        if (req.url === "/admin") {
          res.statusCode = 308;
          res.setHeader("Location", "/admin/");
          res.end();
          return;
        }

        next();
      });
    },
    configurePreviewServer(server) {
      server.middlewares.use((req, res, next) => {
        if (req.url === "/admin") {
          res.statusCode = 308;
          res.setHeader("Location", "/admin/");
          res.end();
          return;
        }

        next();
      });
    }
  };
}

export default defineConfig({
  base: "/admin/",
  plugins: [redirectAdminRoot(), react()],
  server: {
    port: 5173
  }
});
