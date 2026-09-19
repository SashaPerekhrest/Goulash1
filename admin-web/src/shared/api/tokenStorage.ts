const ACCESS_TOKEN_KEY = "portfolio.admin.accessToken";

export const tokenStorage = {
  // MVP trade-off from sprint 07: localStorage is convenient for local admin flows,
  // but the final security pass should revisit token storage hardening.
  get(): string | null {
    return window.localStorage.getItem(ACCESS_TOKEN_KEY);
  },

  set(token: string): void {
    window.localStorage.setItem(ACCESS_TOKEN_KEY, token);
  },

  clear(): void {
    window.localStorage.removeItem(ACCESS_TOKEN_KEY);
  }
};
