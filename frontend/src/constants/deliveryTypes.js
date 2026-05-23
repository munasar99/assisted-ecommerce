export const DELIVERY_TYPES = {
  HOME: "HomeDelivery",
  PICKUP: "Pickup",
};

/** Qoraalka pickup — waa inuu la jaan qaado backend PickupDistrictName */
export const PICKUP_LABEL = "Waa Soodonanaa";

export const DELIVERY_OPTIONS = [
  { value: DELIVERY_TYPES.HOME, label: "Home delivery — guriga lagu keeno" },
  { value: DELIVERY_TYPES.PICKUP, label: PICKUP_LABEL },
];

export function getDeliveryDisplayLabel(type) {
  const opt = DELIVERY_OPTIONS.find((o) => o.value === type);
  return opt?.label ?? type ?? "—";
}

export function isHomeDelivery(type) {
  return type === DELIVERY_TYPES.HOME;
}

export function isPickup(type) {
  return type === DELIVERY_TYPES.PICKUP;
}

export function hasDeliveryChoice(type) {
  return isHomeDelivery(type) || isPickup(type);
}
