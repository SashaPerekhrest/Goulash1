import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  agentRules: false,
  basePath: process.env.NEXT_PUBLIC_BASE_PATH || undefined
};

export default nextConfig;
