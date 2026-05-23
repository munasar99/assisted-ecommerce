function apiOrigin() {
  const apiBase = import.meta.env.VITE_API_URL || "/api";
  if (apiBase.startsWith("http://") || apiBase.startsWith("https://")) {
    return apiBase.replace(/\/api\/?$/i, "");
  }
  return "";
}

/** Turn /uploads/... paths into full URLs (dev: Vite proxy /uploads → backend). */
export function mediaUrl(path) {
  if (!path) return null;
  if (path.startsWith("http://") || path.startsWith("https://")) return path;
  const origin = apiOrigin();
  const p = path.startsWith("/") ? path : `/${path}`;
  return origin ? `${origin}${p}` : p;
}
