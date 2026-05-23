import { mediaUrl } from "../api/mediaUrl";
import StatusBadge from "./StatusBadge";
import {
  getDeliveryDisplayLabel,
  isHomeDelivery,
  isPickup,
  PICKUP_LABEL,
} from "../constants/deliveryTypes";
import { ORDER_STATUSES, STATUS_LABELS } from "../constants/orderStatuses";
import { GK1_LABEL, formatProductQuantity, formatUsd, getOrderPricing } from "../utils/orderPricing";

export default function OrderTrackDetails({ order }) {
  if (!order) return null;

  const pricing = getOrderPricing(order);
  const productImg = mediaUrl(order.orderScreenshotUrl);
  const paymentImg = mediaUrl(order.paymentScreenshotUrl);
  const currentIdx = ORDER_STATUSES.indexOf(order.status);
  const deliveryLabel = getDeliveryDisplayLabel(order.deliveryType);
  const showDistrict = isHomeDelivery(order.deliveryType);

  return (
    <div className="mt-8 space-y-6">
      <div className="card overflow-hidden p-0">
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 bg-slate-50 px-6 py-4">
          <div>
            <p className="font-mono text-lg font-bold text-brand-900">{order.orderId}</p>
            <p className="text-sm text-slate-500">
              {order.customerFullName || "—"}
              {order.customerEmail ? ` · ${order.customerEmail}` : ""}
              {order.customerPhone ? ` · ${order.customerPhone}` : ""}
            </p>
          </div>
          <StatusBadge status={order.status} />
        </div>

        <div className="grid gap-6 p-6 lg:grid-cols-[minmax(0,280px)_1fr]">
          <div className="space-y-3">
            <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
              Sawir alaabta
            </p>
            {productImg ? (
              <a href={productImg} target="_blank" rel="noreferrer" className="block">
                <img
                  src={productImg}
                  alt={order.productName || "Alaabta"}
                  className="max-h-72 w-full rounded-xl border border-slate-200 object-contain bg-slate-50"
                />
              </a>
            ) : (
              <div className="flex aspect-square max-h-48 items-center justify-center rounded-xl border border-dashed border-slate-200 bg-slate-50 text-sm text-slate-400">
                Sawir lama soo rarin
              </div>
            )}
            {paymentImg && (
              <>
                <p className="pt-2 text-xs font-semibold uppercase tracking-wide text-slate-500">
                  Screenshot lacagta
                </p>
                <a href={paymentImg} target="_blank" rel="noreferrer" className="block">
                  <img
                    src={paymentImg}
                    alt="Lacag bixinta"
                    className="max-h-40 w-full rounded-xl border border-slate-200 object-contain bg-slate-50"
                  />
                </a>
              </>
            )}
          </div>

          <div className="space-y-5">
            <section>
              <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-500">
                Alaabta
              </h3>
              <dl className="mt-2 space-y-2 text-sm">
                <Row label="Magaca" value={order.productName || "—"} />
                <Row label="Alaabta" value={formatUsd(pricing.alaabta)} />
                <Row label={GK1_LABEL} value={formatUsd(pricing.gk1Fee ?? pricing.kgFee)} />
                {pricing.quantity > 0 && (
                  <Row label="Tirada alaabta" value={formatProductQuantity(pricing.quantity)} />
                )}
                <Row label="Service Fee" value={formatUsd(pricing.serviceFee)} />
                {order.productUrl && (
                  <div className="flex justify-between gap-4">
                    <dt className="text-slate-500">Link</dt>
                    <dd className="max-w-[60%] truncate text-right">
                      <a
                        href={order.productUrl}
                        target="_blank"
                        rel="noreferrer"
                        className="font-medium text-brand-600 hover:underline"
                      >
                        Fur link alaabta
                      </a>
                    </dd>
                  </div>
                )}
              </dl>
            </section>

            <section>
              <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-500">
                Lacagta
              </h3>
              <dl className="mt-2 space-y-2 text-sm">
                <Row label="Gaarsiinta" value={deliveryLabel} />
                {showDistrict && order.districtName && (
                  <Row label="Degmo" value={order.districtName} />
                )}
                {showDistrict && (
                  <Row label="Delivery fee" value={formatUsd(pricing.deliveryFee)} />
                )}
                {isPickup(order.deliveryType) && (
                  <Row label="Delivery fee" value={`$0.00 (${PICKUP_LABEL})`} />
                )}
                <div className="flex justify-between border-t border-slate-100 pt-2 font-semibold text-slate-900">
                  <span>Wadarta guud</span>
                  <span className="text-brand-700">{formatUsd(pricing.total)}</span>
                </div>
              </dl>
            </section>

            <section>
              <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-500">
                Gaarsiinta & cinwaan
              </h3>
              <dl className="mt-2 space-y-2 text-sm">
                <Row label="Cinwaan" value={order.addressDetail || "—"} />
                {order.notes && <Row label="Faallo" value={order.notes} />}
              </dl>
            </section>

            <section className="text-sm text-slate-500">
              <Row label="Invoice" value={order.invoiceId || "—"} />
              <Row label="User ID" value={order.userId} />
              <Row
                label="Taariikh"
                value={order.createdAt ? new Date(order.createdAt).toLocaleString() : "—"}
              />
            </section>

          </div>
        </div>
      </div>

      <div className="card p-6">
        <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-500">
          Xaaladda dalabka
        </h3>
        <ul className="mt-4 space-y-3">
          {ORDER_STATUSES.map((s, i) => (
            <li
              key={s}
              className={`flex gap-3 text-sm ${i <= currentIdx ? "font-medium text-brand-700" : "text-slate-400"}`}
            >
              <span
                className={`mt-1.5 h-2 w-2 shrink-0 rounded-full ${i <= currentIdx ? "bg-brand-600" : "bg-slate-300"}`}
              />
              {STATUS_LABELS[s]}
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}

function Row({ label, value }) {
  return (
    <div className="flex justify-between gap-4">
      <dt className="text-slate-500">{label}</dt>
      <dd className="text-right font-medium text-slate-900">{value}</dd>
    </div>
  );
}
