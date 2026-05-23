import { useMemo } from "react";
import { useLocation } from "react-router-dom";
import { buildWhatsAppMessage, getStoredOrderId, whatsappUrl } from "../constants/contact";

/** Re-read order id on navigation (same tab after dalab). */
export function useWhatsApp(extraSuffix = "") {
  const location = useLocation();

  return useMemo(() => {
    const orderId = getStoredOrderId();
    let message = buildWhatsAppMessage(orderId);
    if (extraSuffix) message = `${message}\n${extraSuffix}`;
    return { orderId, message, url: whatsappUrl(message) };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- refresh when route changes
  }, [location.pathname, extraSuffix]);
}
