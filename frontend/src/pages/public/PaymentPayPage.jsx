import { Navigate, useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ordersApi } from "../../api/endpoints";
import OrderPaymentForm from "../../components/OrderPaymentForm";
import Loader from "../../components/Loader";
import PageShell from "../../components/PageShell";
import { useCustomerSession } from "../../context/CustomerSessionContext";
import {
  canAccessPayment,
  clearPaymentAccess,
  readCustomerSession,
} from "../../utils/customerSession";
import { getOrderPricing } from "../../utils/orderPricing";

export default function PaymentPayPage() {
  const navigate = useNavigate();
  const { sync } = useCustomerSession();
  const session = readCustomerSession();

  if (!canAccessPayment()) {
    return <Navigate to="/payment" replace />;
  }

  const orderId = session.orderId;
  const phone = session.phone;

  const { data: order, isLoading } = useQuery({
    queryKey: ["track", orderId, phone],
    queryFn: () => ordersApi.track(orderId, phone),
    enabled: !!orderId && !!phone,
  });

  const pricing = order ? getOrderPricing(order) : null;
  const canPay =
    order &&
    pricing &&
    (["InvoiceSent", "WaitingPayment"].includes(order.status) ||
      (order.status === "Pending" && pricing.total > 0));

  if (isLoading) return <Loader />;

  const handleSuccess = (result) => {
    if (result?.emailSent) {
      sessionStorage.setItem("paymentEmailSent", "1");
    } else {
      sessionStorage.removeItem("paymentEmailSent");
    }
    clearPaymentAccess();
    sync();
    navigate("/payment/thanks", { replace: true, state: { emailSent: result?.emailSent } });
  };

  return (
    <PageShell title="Lacag Bixinta" subtitle="Payment" centered maxWidth="max-w-md">
      {canPay && order && (
        <OrderPaymentForm
          orderId={orderId}
          phone={phone}
          order={order}
          onSuccess={handleSuccess}
        />
      )}

      {!canPay && order && (
        <div className="card-elevated space-y-4 p-6 text-center text-sm text-slate-600">
          <p>Lacagta horay ayaa loo soo diray ama dalabka wuu socdaa.</p>
          <button
            type="button"
            onClick={() => navigate("/track")}
            className="btn-primary inline-block w-full"
          >
            Dalabkayga arag
          </button>
        </div>
      )}
    </PageShell>
  );
}
