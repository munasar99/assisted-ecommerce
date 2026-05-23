/** WhatsApp Business — 61 3508774 */
export const LAST_ORDER_KEY = "lastOrder";

export const CONTACT = {
  phoneDisplay: "+252 61 3508774",
  phoneTel: "+252613508774",
  whatsappWaMe: "252613508774",
  email: "E-commerce@gmi.so",
  city: "Muqdisho, Somalia",
};

/** Fariinta default ee WhatsApp */
export const WHATSAPP_GREETING = "Asc Welcome Dalabkaaga Si Dhaqso Ah Ayaa laguu Adeygaa";

export function getStoredOrderId() {
  try {
    const raw = sessionStorage.getItem(LAST_ORDER_KEY);
    if (!raw) return null;
    const data = JSON.parse(raw);
    const id = data?.orderId;
    return typeof id === "string" && id.trim() ? id.trim() : null;
  } catch {
    return null;
  }
}

/** Order ID ku dar haddii macmiilku dalab dhawaan sameeyay */
export function buildWhatsAppMessage(orderId = getStoredOrderId()) {
  if (orderId) {
    return `${WHATSAPP_GREETING}\nOrder ID: ${orderId}`;
  }
  return WHATSAPP_GREETING;
}

export function whatsappUrl(message) {
  const text = encodeURIComponent(message ?? buildWhatsAppMessage());
  return `https://wa.me/${CONTACT.whatsappWaMe}?text=${text}`;
}
