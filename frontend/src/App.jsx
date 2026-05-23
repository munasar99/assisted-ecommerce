import { Navigate, Route, Routes } from "react-router-dom";
import PublicLayout from "./layouts/PublicLayout";
import AdminLayout from "./layouts/AdminLayout";
import ProtectedRoute from "./routes/ProtectedRoute";
import HomePage from "./pages/public/HomePage";
import OrderPage from "./pages/public/OrderPage";
import PaymentPage from "./pages/public/PaymentPage";
import PaymentPayPage from "./pages/public/PaymentPayPage";
import PaymentThankYouPage from "./pages/public/PaymentThankYouPage";
import TrackPage from "./pages/public/TrackPage";
import SuccessPage from "./pages/public/SuccessPage";
import ErrorPage from "./pages/public/ErrorPage";
import AdminLoginPage from "./pages/admin/AdminLoginPage";
import DashboardPage from "./pages/admin/DashboardPage";
import OrdersPage from "./pages/admin/OrdersPage";
import UsersPage from "./pages/admin/UsersPage";
import PaymentsPage from "./pages/admin/PaymentsPage";
import DeliveryPage from "./pages/admin/DeliveryPage";
import AnalyticsPage from "./pages/admin/AnalyticsPage";
import SettingsPage from "./pages/admin/SettingsPage";

export default function App() {
  return (
    <Routes>
      <Route element={<PublicLayout />}>
        <Route index element={<Navigate to="/home" replace />} />
        <Route path="home" element={<HomePage />} />
        <Route path="order" element={<OrderPage />} />
        <Route path="payment" element={<PaymentPage />} />
        <Route path="payment/pay" element={<PaymentPayPage />} />
        <Route path="payment/thanks" element={<PaymentThankYouPage />} />
        <Route path="track" element={<TrackPage />} />
        <Route path="success" element={<SuccessPage />} />
      </Route>
      <Route path="admin/login" element={<AdminLoginPage />} />
      <Route path="admin" element={<ProtectedRoute />}>
        <Route element={<AdminLayout />}>
          <Route index element={<Navigate to="/admin/dashboard" replace />} />
          <Route path="dashboard" element={<DashboardPage />} />
          <Route path="orders" element={<OrdersPage />} />
          <Route path="users" element={<UsersPage />} />
          <Route path="payments" element={<PaymentsPage />} />
          <Route path="delivery" element={<DeliveryPage />} />
          <Route path="analytics" element={<AnalyticsPage />} />
          <Route path="settings" element={<SettingsPage />} />
        </Route>
      </Route>
      <Route path="*" element={<ErrorPage />} />
    </Routes>
  );
}
