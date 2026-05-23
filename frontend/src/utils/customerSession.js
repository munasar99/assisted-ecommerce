import { LAST_ORDER_KEY } from "../constants/contact";
import { normalizePhone } from "./phone";
import { validatePhone } from "./validation";

const TRACK_AUTH_KEY = "ubaxTrackAuth";

function readRaw(key) {
  try {
    return localStorage.getItem(key);
  } catch {
    return null;
  }
}

function writeRaw(key, value) {
  try {
    localStorage.setItem(key, value);
    if (key === LAST_ORDER_KEY) sessionStorage.removeItem(LAST_ORDER_KEY);
  } catch {
    // ignore
  }
}

export function readTrackAuth() {
  try {
    const raw = readRaw(TRACK_AUTH_KEY);
    if (!raw) return null;
    const data = JSON.parse(raw);
    if (!data?.orderId) return null;
    if (data.phone) data.phone = normalizePhone(data.phone);
    if (!data.phone || !validatePhone(data.phone)) return null;
    if (data.loggedIn === false) return null;
    return data;
  } catch {
    return null;
  }
}

export function persistTrackAuth(orderId, phone) {
  const id = String(orderId ?? "").trim();
  const ph = normalizePhone(phone);
  if (!id || !validatePhone(ph)) return null;

  const auth = {
    orderId: id,
    phone: ph,
    loggedIn: true,
    savedAt: Date.now(),
  };

  writeRaw(TRACK_AUTH_KEY, JSON.stringify(auth));
  saveCustomerSession({ orderId: id, phone: ph, loggedIn: true });
  return auth;
}

export function clearTrackAuth() {
  try {
    localStorage.removeItem(TRACK_AUTH_KEY);
  } catch {
    // ignore
  }
  const s = readCustomerSession();
  if (s) saveCustomerSession({ loggedIn: false });
}

export function readCustomerSession() {
  const auth = readTrackAuth();
  if (auth) return { ...readOrderSession(), ...auth };

  try {
    let raw = readRaw(LAST_ORDER_KEY);
    if (!raw) {
      raw = sessionStorage.getItem(LAST_ORDER_KEY);
      if (raw) {
        writeRaw(LAST_ORDER_KEY, raw);
      }
    }
    if (!raw) return null;
    const data = JSON.parse(raw);
    if (!data?.orderId) return null;
    if (data.phone) data.phone = normalizePhone(data.phone);
    if (!data.phone) return null;
    return data;
  } catch {
    return null;
  }
}

function readOrderSession() {
  try {
    const raw = readRaw(LAST_ORDER_KEY);
    if (!raw) return {};
    return JSON.parse(raw);
  } catch {
    return {};
  }
}

export function saveCustomerSession(patch) {
  const prev = { ...readOrderSession(), ...readTrackAuth() };
  const next = { ...prev, ...patch, updatedAt: Date.now() };
  if (next.phone) next.phone = normalizePhone(next.phone);
  if (next.orderId) next.orderId = String(next.orderId).trim();
  writeRaw(LAST_ORDER_KEY, JSON.stringify(next));
  if (next.loggedIn !== false && next.orderId && next.phone && validatePhone(next.phone)) {
    writeRaw(
      TRACK_AUTH_KEY,
      JSON.stringify({
        orderId: next.orderId,
        phone: next.phone,
        loggedIn: true,
        savedAt: Date.now(),
      }),
    );
  }
  return next;
}

export function saveAfterOrder(order) {
  const ph = normalizePhone(order.phone);
  persistTrackAuth(order.orderId, ph);
  return saveCustomerSession({
    orderId: order.orderId,
    userId: order.userId,
    phone: ph,
    deliveryType: order.deliveryType,
    districtId: order.districtId,
    districtName: order.districtName,
    deliveryFee: order.deliveryFee,
    loggedIn: true,
    paymentAllowed: true,
  });
}

export function markCustomerLoggedIn(orderId, phone) {
  return persistTrackAuth(orderId, phone);
}

export function clearPaymentAccess() {
  saveCustomerSession({ paymentAllowed: false, loggedIn: true });
}

export function customerLogout() {
  clearTrackAuth();
  saveCustomerSession({ loggedIn: false, paymentAllowed: false });
}

export function canAccessPayment() {
  const s = readCustomerSession();
  return !!(s?.paymentAllowed && s.orderId && s.phone && validatePhone(s.phone));
}

export function isCustomerLoggedIn() {
  return !!readTrackAuth();
}

export function getTrackSessionBoot() {
  const auth = readTrackAuth();
  const ok = !!auth;
  return {
    orderId: auth?.orderId ?? "",
    phone: auth?.phone ?? "",
    searchOrderId: ok ? auth.orderId : "",
    searchPhone: ok ? auth.phone : "",
    showForm: !ok,
  };
}
