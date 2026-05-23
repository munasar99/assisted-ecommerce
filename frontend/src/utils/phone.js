/** Waa inuu la jaan qaado backend PhoneNormalizer */
export function normalizePhone(phone) {
  if (!phone) return "";
  let digits = String(phone).replace(/\D/g, "");
  if (!digits) return "";
  if (digits.startsWith("252") && digits.length >= 12) digits = digits.slice(3);
  else if (digits.startsWith("252") && digits.length > 9) digits = digits.slice(3);
  if (digits.startsWith("0") && digits.length > 9) digits = digits.slice(1);
  if (digits.length > 9) digits = digits.slice(-9);
  if (digits.length < 9) return `+${digits}`;
  return `+252${digits}`;
}

/** Hal user telefoonkiisa — ka reeb duplicates liiska. */
export function dedupeUsersByPhone(users) {
  const byPhone = new Map();
  for (const u of users ?? []) {
    const key = normalizePhone(u.phone);
    if (!key) {
      byPhone.set(u.userId, u);
      continue;
    }
    const prev = byPhone.get(key);
    if (!prev) {
      byPhone.set(key, u);
      continue;
    }
    const pick =
      (u.totalOrders ?? 0) > (prev.totalOrders ?? 0) ? u : prev;
    byPhone.set(key, pick);
  }
  return [...byPhone.values()];
}
