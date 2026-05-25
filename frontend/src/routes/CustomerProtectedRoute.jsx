import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useCustomerSession } from "../context/CustomerSessionContext";

/** Bogagga macmiilka — login loo baahan yahay; sessionStorage (tab) kaliya. */
export default function CustomerProtectedRoute() {
  const { loggedIn } = useCustomerSession();
  const location = useLocation();

  if (!loggedIn) {
    return <Navigate to="/track" replace state={{ from: location.pathname, requireLogin: true }} />;
  }

  return <Outlet />;
}
