import { createContext, useContext, useEffect, useState } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { api, getToken, setToken } from "./api";

const AuthContext = createContext(null);
export const useAuth = () => useContext(AuthContext);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(Boolean(getToken()));

  // Restore the session from a saved token.
  useEffect(() => {
    if (!getToken()) return;
    api
      .me()
      .then(setUser)
      .catch(() => setToken(null))
      .finally(() => setLoading(false));
  }, []);

  // The API client fires this when the server rejects our token.
  useEffect(() => {
    const onExpired = () => setUser(null);
    window.addEventListener("auth:expired", onExpired);
    return () => window.removeEventListener("auth:expired", onExpired);
  }, []);

  const finish = (response) => {
    setToken(response.token);
    setUser(response.user);
    return response.user;
  };

  const value = {
    user,
    loading,
    login: async (email, password) => finish(await api.login({ email, password })),
    register: async (fullName, email, password) => finish(await api.register({ fullName, email, password })),
    logout: () => {
      setToken(null);
      setUser(null);
    },
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

/** Wrap a page so signed-out visitors are sent to the login page and then back. */
export function RequireAuth({ children }) {
  const { user, loading } = useAuth();
  const location = useLocation();

  if (loading) return <p className="muted">Loading…</p>;
  if (!user) return <Navigate to="/login" state={{ from: location.pathname + location.search }} replace />;
  return children;
}
