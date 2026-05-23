/** Khidmad adeegga: $1 hal mar dalab kasta. */
export const SERVICE_FEE_FLAT_USD = 1;

/** GK1: $1 hal mar dalab — gooni alaabta, tirada kuma dhufto. */
export const GK1_FEE_USD = 1;
export const GK1_LABEL = "GK1";
/** @deprecated use GK1_FEE_USD */
export const KG_FEE_PER_KG_USD = GK1_FEE_USD;

/** @param {string | null | undefined} notes */
export function parseUnitPriceFromNotes(notes) {
  const m = notes?.match(/Product price:\s*\$?([\d.]+)/i);
  return m ? Number(m[1]) : 0;
}

/** @param {object | null | undefined} order */
export function getOrderPricing(order) {
  if (!order) {
    return {
      unitPrice: 0,
      quantity: 0,
      alaabta: 0,
      productSubtotal: 0,
      kgFee: 0,
      serviceFee: 0,
      deliveryFee: 0,
      total: 0,
    };
  }

  const quantity = order.quantity ?? 0;
  const qty = Math.max(0, Math.floor(Number(quantity) || 0));
  const unitPrice =
    order.productUnitPriceUsd > 0
      ? order.productUnitPriceUsd
      : parseUnitPriceFromNotes(order.notes);

  const alaabta = Math.round(unitPrice * 100) / 100;
  const gk1Fee =
    typeof order.kgFeeUsd === "number" && order.kgFeeUsd >= 0
      ? order.kgFeeUsd
      : GK1_FEE_USD;

  const serviceFee =
    order.serviceFeeUsd > 0 ? order.serviceFeeUsd : SERVICE_FEE_FLAT_USD;
  const deliveryFee = order.deliveryFee ?? 0;
  const total = alaabta + gk1Fee + serviceFee + deliveryFee;

  return {
    unitPrice,
    quantity: qty,
    alaabta,
    productSubtotal: alaabta,
    kgFee: gk1Fee,
    gk1Fee,
    serviceFee,
    deliveryFee,
    total,
  };
}

export function formatUsd(amount) {
  return `$${Number(amount || 0).toFixed(2)}`;
}

/** Tirada alaabta — ma muujin KG macmiilka. */
export function formatProductQuantity(quantity) {
  const n = Math.max(0, Math.floor(Number(quantity) || 0));
  if (n <= 0) return "0";
  return n === 1 ? "1 alaab" : `${n} alaab`;
}

/** @deprecated Use formatProductQuantity */
export function formatQuantityKg(quantity) {
  return formatProductQuantity(quantity);
}

export function formatQuantityFeeLine(quantity, feeUsd) {
  const n = Math.max(1, Math.floor(Number(quantity) || 0));
  const unit = formatUsd(KG_FEE_PER_KG_USD);
  return n === 1 ? `1 alaab × ${unit}` : `${n} alaab × ${unit} = ${formatUsd(feeUsd)}`;
}

/** @deprecated */
export function formatKgFeeLine(quantity, kgFee) {
  return formatQuantityFeeLine(quantity, kgFee);
}

export function formatServiceFeeDetail(_quantity, serviceFeeUsd) {
  return formatUsd(serviceFeeUsd > 0 ? serviceFeeUsd : SERVICE_FEE_FLAT_USD);
}

/** Lacag bixinta: GK1 + alaabta + service + gaarsiin. */
export function getPaymentPricing(order) {
  return getOrderPricing(order);
}
