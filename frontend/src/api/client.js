import axios from "axios";

const BASE = import.meta.env.VITE_API_URL || "http://localhost:5298/api";

export const ADMIN_TOKEN_KEY = "adminToken";
export const ADMIN_PROFILE_KEY = "adminProfile";

export function getAdminToken() {
  return localStorage.getItem(ADMIN_TOKEN_KEY) || sessionStorage.getItem(ADMIN_TOKEN_KEY);
}

export function setAdminToken(token) {
  if (token) {
    localStorage.setItem(ADMIN_TOKEN_KEY, token);
    sessionStorage.setItem(ADMIN_TOKEN_KEY, token);
  }
}

export function clearAdminToken() {
  localStorage.removeItem(ADMIN_TOKEN_KEY);
  sessionStorage.removeItem(ADMIN_TOKEN_KEY);
  localStorage.removeItem(ADMIN_PROFILE_KEY);
  sessionStorage.removeItem(ADMIN_PROFILE_KEY);
}

// Migrate old session-only storage
(function migrateAdminStorage() {
  const legacy = sessionStorage.getItem(ADMIN_TOKEN_KEY);
  if (legacy && !localStorage.getItem(ADMIN_TOKEN_KEY)) {
    localStorage.setItem(ADMIN_TOKEN_KEY, legacy);
    const profile = sessionStorage.getItem(ADMIN_PROFILE_KEY);
    if (profile) localStorage.setItem(ADMIN_PROFILE_KEY, profile);
  }
})();

export const api = axios.create({
  baseURL: BASE,
  headers: { "Content-Type": "application/json" },
});

api.interceptors.request.use((config) => {
  const token = getAdminToken();
  if (token) config.headers.Authorization = `Bearer ${token}`;
  // FormData: ha isticmaalin application/json (waxay keentaa HTTP 415)
  if (config.data instanceof FormData) {
    if (typeof config.headers.delete === "function") {
      config.headers.delete("Content-Type");
    } else {
      delete config.headers["Content-Type"];
    }
  }
  return config;
});

api.interceptors.response.use(
  (res) => res,
  (err) => {
    const status = err.response?.status;
    if (status === 401 && getAdminToken()) {
      clearAdminToken();
      if (!window.location.pathname.startsWith("/admin/login")) {
        window.location.href = "/admin/login?expired=1";
      }
    }
    const isNetwork =
      !err.response &&
      (err.code === "ERR_NETWORK" ||
        err.message === "Network Error" ||
        err.message?.includes("Network Error"));

    const message = isNetwork
      ? "API lama helin. Hubi backend inuu socdo: http://localhost:5298 (dotnet run) iyo MongoDB."
      : status === 415
        ? "Cilad upload (415). Dib u cusboonaysii bogga (F5) oo mar kale isku day."
        : err.response?.data?.message ||
          err.response?.data?.title ||
          (status === 401 ? "Session-ka waa dhacay. Mar kale gal." : null) ||
          err.message ||
          "Request failed";
    return Promise.reject(new Error(message));
  },
);

export const unwrap = (res) => res.data?.data ?? res.data;
