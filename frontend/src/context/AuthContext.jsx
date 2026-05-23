import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { authApi } from "../api/endpoints";
import {
  ADMIN_PROFILE_KEY,
  clearAdminToken,
  getAdminToken,
  setAdminToken,
} from "../api/client";

const AuthContext = createContext(null);

function readStoredProfile() {
  try {
    const raw =
      localStorage.getItem(ADMIN_PROFILE_KEY) || sessionStorage.getItem(ADMIN_PROFILE_KEY);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

function saveProfile(admin) {
  const raw = JSON.stringify(admin);
  localStorage.setItem(ADMIN_PROFILE_KEY, raw);
  sessionStorage.setItem(ADMIN_PROFILE_KEY, raw);
}

export function AuthProvider({ children }) {
  const [admin, setAdmin] = useState(() => readStoredProfile());
  const [token, setToken] = useState(() => getAdminToken());
  const [ready, setReady] = useState(false);

  const logout = useCallback(async () => {
    try {
      if (getAdminToken()) await authApi.logout();
    } catch {
      // token expired or invalid
    } finally {
      clearAdminToken();
      setToken(null);
      setAdmin(null);
    }
  }, []);

  useEffect(() => {
    let cancelled = false;

    async function bootstrap() {
      const stored = getAdminToken();
      if (!stored) {
        if (!cancelled) setReady(true);
        return;
      }

      try {
        const profile = await authApi.profile();
        if (!cancelled) {
          saveProfile(profile);
          setAdmin(profile);
          setToken(stored);
        }
      } catch {
        clearAdminToken();
        if (!cancelled) {
          setToken(null);
          setAdmin(null);
        }
      } finally {
        if (!cancelled) setReady(true);
      }
    }

    bootstrap();
    return () => {
      cancelled = true;
    };
  }, []);

  const login = async (email, password) => {
    const data = await authApi.login({ email, password });
    setAdminToken(data.accessToken);
    saveProfile(data.admin);
    setToken(data.accessToken);
    setAdmin(data.admin);
    return data;
  };

  const value = useMemo(
    () => ({
      admin,
      token,
      ready,
      isAuthenticated: !!token && !!admin,
      login,
      logout,
    }),
    [admin, token, ready, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export const useAuth = () => useContext(AuthContext);
