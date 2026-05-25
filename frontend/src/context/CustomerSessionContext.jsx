import { createContext, useCallback, useContext, useMemo, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { ordersApi } from "../api/endpoints";
import {
  customerLogout,
  persistTrackAuth,
  readCustomerSession,
  readTrackAuth,
  saveCustomerSession,
} from "../utils/customerSession";
import { normalizePhone } from "../utils/phone";
import { validatePhone } from "../utils/validation";

const CustomerSessionContext = createContext(null);

export function CustomerSessionProvider({ children }) {
  const qc = useQueryClient();
  const [session, setSession] = useState(() => readCustomerSession());

  const sync = useCallback(() => {
    setSession(readCustomerSession());
  }, []);

  const login = useCallback(
    async (orderId, phone) => {
      const id = String(orderId ?? "").trim();
      const ph = normalizePhone(phone);
      if (!id || !validatePhone(ph)) return { ok: false, message: "Order ID iyo telefoon sax ah geli" };

      try {
        const order = await ordersApi.track(id, ph);
        const next = persistTrackAuth(id, ph);
        if (!next) return { ok: false, message: "Login lama keydiyin" };

        saveCustomerSession({
          orderId: order.orderId,
          userId: order.userId,
          phone: ph,
          deliveryType: order.deliveryType,
          districtId: order.districtId,
          districtName: order.districtName,
          deliveryFee: order.deliveryFee,
          loggedIn: true,
        });

        qc.removeQueries({ queryKey: ["track"] });
        const merged = readCustomerSession();
        setSession(merged);
        return { ok: true, order };
      } catch (err) {
        return {
          ok: false,
          message: err?.message || "Dalab lama helin. Hubi Order ID iyo telefoon.",
        };
      }
    },
    [qc],
  );

  const logout = useCallback(() => {
    customerLogout();
    qc.removeQueries({ queryKey: ["track"] });
    setSession(null);
  }, [qc]);

  const patch = useCallback(
    (data) => {
      saveCustomerSession(data);
      setSession(readCustomerSession());
    },
    [],
  );

  const auth = session?.loggedIn !== false ? readTrackAuth() : null;
  const loggedIn = !!(
    auth?.orderId &&
    auth?.phone &&
    validatePhone(auth.phone)
  );

  const value = useMemo(
    () => ({
      session: session ?? auth,
      loggedIn,
      orderId: auth?.orderId ?? session?.orderId ?? "",
      phone: auth?.phone ?? session?.phone ?? "",
      login,
      logout,
      sync,
      patch,
    }),
    [session, auth, loggedIn, login, logout, sync, patch],
  );

  return (
    <CustomerSessionContext.Provider value={value}>{children}</CustomerSessionContext.Provider>
  );
}

export function useCustomerSession() {
  const ctx = useContext(CustomerSessionContext);
  if (!ctx) throw new Error("useCustomerSession must be used within CustomerSessionProvider");
  return ctx;
}
