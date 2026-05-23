import { createContext, useCallback, useContext, useMemo, useState } from "react";
import {
  customerLogout,
  persistTrackAuth,
  readCustomerSession,
  readTrackAuth,
  saveCustomerSession,
} from "../utils/customerSession";
import { validatePhone } from "../utils/validation";

const CustomerSessionContext = createContext(null);

export function CustomerSessionProvider({ children }) {
  const [session, setSession] = useState(() => readCustomerSession());

  const sync = useCallback(() => {
    setSession(readCustomerSession());
  }, []);

  const login = useCallback((orderId, phone) => {
    const next = persistTrackAuth(orderId, phone);
    if (next) setSession({ ...readCustomerSession(), ...next });
    return next;
  }, []);

  const logout = useCallback(() => {
    customerLogout();
    setSession(readCustomerSession());
  }, []);

  const patch = useCallback((data) => {
    const next = saveCustomerSession(data);
    setSession(readCustomerSession());
    return next;
  }, []);

  const auth = readTrackAuth();
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
