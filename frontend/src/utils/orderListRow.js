/** Normalize API row fields (camelCase or legacy). */
export function orderListRow(row) {
  if (!row) return {};
  return {
    orderId: row.orderId ?? row.OrderId ?? "",
    userId: row.userId ?? row.UserId ?? "",
    customerFullName: row.customerFullName ?? row.CustomerFullName ?? "",
    customerPhone: row.customerPhone ?? row.CustomerPhone ?? "",
    customerEmail: row.customerEmail ?? row.CustomerEmail ?? "",
    productName: row.productName ?? row.ProductName ?? "",
    status: row.status ?? row.Status ?? "",
  };
}
