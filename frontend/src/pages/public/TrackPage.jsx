import { useEffect, useState } from "react";
import { useLocation } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ordersApi } from "../../api/endpoints";
import OrderTrackDetails from "../../components/OrderTrackDetails";
import Loader from "../../components/Loader";
import PageShell from "../../components/PageShell";
import { useCustomerSession } from "../../context/CustomerSessionContext";
import { useToast } from "../../context/ToastContext";
import { validatePhone } from "../../utils/validation";

export default function TrackPage() {
  const { show } = useToast();
  const location = useLocation();
  const {
    login,
    logout,
    patch,
    sync,
    loggedIn,
    orderId: ctxOrderId,
    phone: ctxPhone,
  } = useCustomerSession();

  const [manualLogin, setManualLogin] = useState(() => !loggedIn);
  const [formOrderId, setFormOrderId] = useState("");
  const [formPhone, setFormPhone] = useState("");
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (location.state?.requireLogin) {
      setManualLogin(true);
      show("Fadlan login samee (Order ID + telefoon) si aad u aragto dalabkaaga.", "error");
    }
  }, [location.state?.requireLogin, show]);

  useEffect(() => {
    const restore = () => {
      sync();
      if (!loggedIn) setManualLogin(true);
    };
    restore();
    window.addEventListener("pageshow", restore);
    return () => window.removeEventListener("pageshow", restore);
  }, [sync, loggedIn]);

  const autoView = loggedIn && !manualLogin;
  const activeOrderId = autoView ? ctxOrderId : "";
  const activePhone = autoView ? ctxPhone : "";

  const {
    data: order,
    isLoading,
    isFetching,
  } = useQuery({
    queryKey: ["track", activeOrderId, activePhone],
    queryFn: () => ordersApi.track(activeOrderId, activePhone),
    enabled: autoView && !!activeOrderId && !!activePhone,
    staleTime: 60_000,
  });

  useEffect(() => {
    if (!order || !autoView) return;
    patch({
      orderId: order.orderId,
      userId: order.userId,
      phone: activePhone,
      deliveryType: order.deliveryType,
      districtId: order.districtId,
      districtName: order.districtName,
      deliveryFee: order.deliveryFee,
      loggedIn: true,
    });
  }, [order, activePhone, autoView, patch]);

  const track = async (e) => {
    e.preventDefault();
    if (!formOrderId.trim() || !validatePhone(formPhone)) {
      show("Order ID iyo phone sax ah geli", "error");
      return;
    }
    setSubmitting(true);
    try {
      const result = await login(formOrderId, formPhone);
      if (!result.ok) {
        show(result.message, "error");
        return;
      }
      setManualLogin(false);
      show("Dalabka waa la helay", "success");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <PageShell
      title=" Kusoo dhawow bogga login-ka"
      subtitle={manualLogin ? "Geli Order ID iyo telefoonka" : undefined}
    >
      {manualLogin && (
        <form
          onSubmit={track}
          className="card-elevated mx-auto max-w-md space-y-4 p-6"
        >
          <label className="block text-sm font-medium text-slate-700">
            Order ID
          </label>
          <input
            className="input-field"
            placeholder="Enter your order ID"
            value={formOrderId}
            onChange={(e) => setFormOrderId(e.target.value)}
            autoComplete="off"
          />
          <label className="block text-sm font-medium text-slate-700">
            Telefoon
          </label>
          <input
            className="input-field"
            type="tel"
            placeholder="+25261..."
            value={formPhone}
            onChange={(e) => setFormPhone(e.target.value)}
            autoComplete="tel"
          />
          <button
            type="submit"
            disabled={submitting}
            className="btn-primary w-full !py-3.5 text-base font-semibold disabled:opacity-60"
          >
            {submitting ? "Waa la hubinayaa…" : "Submit"}
          </button>
        </form>
      )}

      {autoView && (isLoading || isFetching) && !order && <Loader />}

      {autoView && order && (
        <>
          <OrderTrackDetails order={order} />
          <div className="mt-6 flex flex-wrap gap-4 text-sm">
            <button
              type="button"
              onClick={() => {
                setManualLogin(true);
                setFormOrderId("");
                setFormPhone("");
              }}
              className="text-brand-600 hover:underline"
            >
              Raadi dalab kale
            </button>
            <button
              type="button"
              onClick={() => {
                logout();
                setManualLogin(true);
                setFormOrderId("");
                setFormPhone("");
              }}
              className="text-slate-500 hover:underline"
            >
              Ka bax
            </button>
          </div>
        </>
      )}

      {autoView && !isLoading && !isFetching && !order && (
        <div className="card-elevated mx-auto max-w-md p-6 text-center text-sm text-slate-600">
          <p>
            Dalab lama helin. Hubi Order ID ({ctxOrderId || activeOrderId}) iyo
            telefoon.
          </p>
          <button
            type="button"
            onClick={() => {
              logout();
              setManualLogin(true);
            }}
            className="btn-primary mt-4 w-full"
          >
            Dib u geli
          </button>
        </div>
      )}
    </PageShell>
  );
}
