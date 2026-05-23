/** Bog kasta → API endpoint (tijaabi api.http ama Swagger) */
export const API_PAGE_LINKS = [
  { page: "/home", label: "Home", method: "GET", path: "/api/health", auth: false },
  { page: "/order", label: "Order", method: "GET", path: "/api/delivery/zones/active", auth: false },
  { page: "/order", label: "Order", method: "POST", path: "/api/orders", auth: false },
  { page: "/order", label: "Order", method: "POST", path: "/api/uploads/order", auth: false },
  { page: "/payment", label: "Payment", method: "GET", path: "/api/orders/track", auth: false },
  { page: "/payment", label: "Payment", method: "POST", path: "/api/payments/upload", auth: false },
  { page: "/track", label: "Track", method: "GET", path: "/api/orders/track", auth: false },
  { page: "/admin/login", label: "Admin login", method: "POST", path: "/api/auth/login", auth: false },
  { page: "/admin/dashboard", label: "Dashboard", method: "GET", path: "/api/analytics/dashboard", auth: true },
  { page: "/admin/orders", label: "Orders", method: "GET", path: "/api/orders", auth: true },
  { page: "/admin/orders", label: "Orders", method: "PATCH", path: "/api/orders/{orderId}/status", auth: true },
  { page: "/admin/users", label: "Users", method: "GET", path: "/api/users", auth: true },
  { page: "/admin/users", label: "Users", method: "POST", path: "/api/users", auth: true },
  { page: "/admin/payments", label: "Payments", method: "GET", path: "/api/orders?status=PaymentReview", auth: true },
  { page: "/admin/delivery", label: "Delivery", method: "GET", path: "/api/delivery/zones", auth: true },
  { page: "/admin/analytics", label: "Analytics", method: "GET", path: "/api/analytics/dashboard", auth: true },
];
