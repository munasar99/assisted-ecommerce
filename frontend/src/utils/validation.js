const PHONE_RE = /^(\+252|252|0)?[67]\d{8}$/;

export function validatePhone(phone) {
  const n = phone.replace(/[\s-]/g, "");
  return PHONE_RE.test(n);
}

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function validateEmail(email) {
  if (!email || !String(email).trim()) return false;
  return EMAIL_RE.test(String(email).trim());
}

export function validateUrl(url) {
  try {
    const u = new URL(url);
    return u.protocol === "http:" || u.protocol === "https:";
  } catch {
    return false;
  }
}

export function validateImageFile(file, maxMb = 5) {
  if (!file) return "File required";
  const allowed = ["image/jpeg", "image/jpg", "image/png", "image/webp"];
  if (!allowed.includes(file.type)) return "JPG, PNG, WEBP only";
  if (file.size > maxMb * 1024 * 1024) return `Max ${maxMb}MB`;
  return null;
}
