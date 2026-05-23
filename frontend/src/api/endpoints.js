import { api, unwrap } from "./client";

export const authApi = {
  login: (body) => api.post("/auth/login", body).then(unwrap),
  logout: () => api.post("/auth/logout"),
  profile: () => api.get("/auth/profile").then(unwrap),
};

export const ordersApi = {
  create: (body) => api.post("/orders", body).then(unwrap),
  track: (orderId, phone) =>
    api.get("/orders/track", { params: { orderId, phone } }).then(unwrap),
  list: (params) => api.get("/orders", { params }).then(unwrap),
  get: (orderId) => api.get(`/orders/${orderId}`).then(unwrap),
  update: (orderId, body) => api.put(`/orders/${orderId}`, body).then(unwrap),
  remove: (orderId) => api.delete(`/orders/${orderId}`).then(unwrap),
  updateStatus: (orderId, body) =>
    api.patch(`/orders/${orderId}/status`, body).then(unwrap),
  createInvoice: (orderId, body) =>
    api.post(`/orders/${orderId}/invoice`, body).then(unwrap),
};

export const usersApi = {
  create: (body) => api.post("/users", body).then(unwrap),
  list: (params) => api.get("/users", { params }).then(unwrap),
  get: (userId) => api.get(`/users/${userId}`).then(unwrap),
  update: (userId, body) => api.put(`/users/${userId}`, body).then(unwrap),
  remove: (userId) => api.delete(`/users/${userId}`).then(unwrap),
  updateStatus: (userId, body) =>
    api.patch(`/users/${userId}/status`, body).then(unwrap),
};

const zonePath = (id) => `/delivery/zones/${encodeURIComponent(id)}`;

export const deliveryApi = {
  active: () => api.get("/delivery/zones/active").then(unwrap),
  all: () => api.get("/delivery/zones").then(unwrap),
  get: (id) => api.get(zonePath(id)).then(unwrap),
  create: (body) => api.post("/delivery/zones", body).then(unwrap),
  update: (id, body) => api.put(zonePath(id), body).then(unwrap),
  remove: (id) => api.delete(zonePath(id)).then(unwrap),
  updateFee: (id, feeUsd) => api.put(`${zonePath(id)}/fee`, { feeUsd }).then(unwrap),
  toggle: (id) => api.patch(`${zonePath(id)}/toggle`).then(unwrap),
};

export const paymentsApi = {
  list: (params) => api.get("/payments", { params }).then(unwrap),
  get: (paymentId) => api.get(`/payments/${paymentId}`).then(unwrap),
  create: (body) => api.post("/payments", body).then(unwrap),
  update: (paymentId, body) => api.put(`/payments/${paymentId}`, body).then(unwrap),
  remove: (paymentId) => api.delete(`/payments/${paymentId}`).then(unwrap),
  upload: (formData) => api.post("/payments/upload", formData).then(unwrap),
};

export const uploadsApi = {
  orderScreenshot: (formData) => api.post("/uploads/order", formData).then(unwrap),
};

export const notificationsApi = {
  emailStatus: () => api.get("/notifications/email/status").then(unwrap),
  sendTestEmail: (body) => api.post("/notifications/email/test", body).then(unwrap),
  sendOrderEmail: (orderId) => api.post(`/notifications/email/order/${encodeURIComponent(orderId)}`).then(unwrap),
};

export const analyticsApi = {
  dashboard: () => api.get("/analytics/dashboard").then(unwrap),
};
