import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { paymentsApi } from "../api/endpoints";
import UploadInput from "./UploadInput";
import { useToast } from "../context/ToastContext";
import { COMPANY_PAYMENT_NUMBER, PAYMENT_METHODS } from "../constants/orderStatuses";
import { GK1_LABEL, formatUsd, getPaymentPricing } from "../utils/orderPricing";
import { validatePhone } from "../utils/validation";

export default function OrderPaymentForm({ orderId, phone, order, onSuccess }) {
  const { show } = useToast();
  const qc = useQueryClient();
  const [method, setMethod] = useState(PAYMENT_METHODS[0]);
  const [payerPhone, setPayerPhone] = useState("");
  const [payerPhoneErr, setPayerPhoneErr] = useState("");
  const [file, setFile] = useState(null);
  const [fileErr, setFileErr] = useState("");
  const [uploading, setUploading] = useState(false);

  const pricing = getPaymentPricing(order);
  const canPay =
    order &&
    (["InvoiceSent", "WaitingPayment"].includes(order.status) ||
      (order.status === "Pending" && pricing.total > 0));

  if (!canPay) return null;

  const sendPayment = async (e) => {
    e.preventDefault();
    if (!validatePhone(payerPhone)) {
      setPayerPhoneErr("Geli lambarka saxda ah (+2526...)");
      return;
    }
    if (!file) {
      setFileErr("Soo rar sawirka lacagta");
      return;
    }
    setUploading(true);
    try {
      const fd = new FormData();
      fd.append("orderId", orderId);
      fd.append("phone", phone);
      fd.append("payerPhone", payerPhone.trim());
      fd.append("paymentMethod", method);
      fd.append("file", file);
      const result = await paymentsApi.upload(fd);
      await qc.invalidateQueries({ queryKey: ["track", orderId, phone] });
      show("Lacagta waa la helay — waad ku mahadsan tahay!", "success");
      if (result?.emailSent) {
        setTimeout(
          () => show("Email xaqiijin ah ayaa loo diray cinwaankaaga.", "success"),
          500,
        );
      } else if (result?.emailError) {
        setTimeout(
          () =>
            show(`Email lama dirin: ${result.emailError}`, "error"),
          600,
        );
      }
      onSuccess?.(result);
    } catch (err) {
      const msg =
        err.message ||
        "Screenshot-ka lama aqbalin. Hubi inuu muujiyo lacagta saxda ah.";
      setFile(null);
      setFileErr(msg);
      show(msg, "error");
    } finally {
      setUploading(false);
    }
  };

  return (
    <form onSubmit={sendPayment} className="card-elevated space-y-5 p-6">
      <p className="font-mono text-sm text-slate-500">{orderId}</p>
      <h3 className="text-lg font-semibold text-slate-900">Lacag bixinta</h3>
      <div className="rounded-xl border border-slate-200 bg-slate-50 p-4 text-sm text-slate-800">
        <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
          Xisaabta (otomatik)
        </p>
        <dl className="mt-3 space-y-2">
          <div className="flex justify-between gap-3">
            <dt className="text-slate-600">Alaabta</dt>
            <dd className="font-medium">{formatUsd(pricing.alaabta)}</dd>
          </div>
          <div className="flex justify-between gap-3">
            <dt className="font-medium text-slate-700">{GK1_LABEL}</dt>
            <dd className="font-medium">{formatUsd(pricing.gk1Fee ?? pricing.kgFee)}</dd>
          </div>
          <div className="flex justify-between gap-3">
            <dt className="text-slate-600">Service Fee</dt>
            <dd className="font-medium">{formatUsd(pricing.serviceFee)}</dd>
          </div>
          {pricing.deliveryFee > 0 && (
            <div className="flex justify-between gap-3">
              <dt className="text-slate-600">Gaarsiin</dt>
              <dd className="font-medium">{formatUsd(pricing.deliveryFee)}</dd>
            </div>
          )}
          <div className="flex justify-between gap-3 border-t border-slate-200 pt-2 font-semibold text-brand-900">
            <dt>Wadarta lacagta</dt>
            <dd>{formatUsd(pricing.total)}</dd>
          </div>
        </dl>
      </div>
      <p className="text-sm text-slate-600">
        U dir <strong>{formatUsd(pricing.total)}</strong> kadib soo rar screenshot.
      </p>

      <div>
        <label className="mb-1 block text-sm font-medium text-slate-700">
          Habka lacag bixinta
        </label>
        <select
          className="input-field"
          value={method}
          onChange={(e) => setMethod(e.target.value)}
        >
          {PAYMENT_METHODS.map((m) => (
            <option key={m}>{m}</option>
          ))}
        </select>
      </div>

      <div className="rounded-xl border border-brand-200 bg-brand-50 p-4 text-center">
        <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
          Lambarka shirkadda
        </p>
        <p className="mt-2 text-xl font-bold text-brand-900">{COMPANY_PAYMENT_NUMBER}</p>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium text-slate-700">
          Lambarkaaga (ka bixisay) *
        </label>
        <input
          type="tel"
          className="input-field"
          placeholder="+25261..."
          value={payerPhone}
          onChange={(e) => {
            setPayerPhone(e.target.value);
            setPayerPhoneErr("");
          }}
        />
        {payerPhoneErr && <p className="mt-1 text-sm text-red-600">{payerPhoneErr}</p>}
      </div>

      <UploadInput
        label={`Screenshot lacagta * (waa inuu muujiyaa ${formatUsd(pricing.total)})`}
        onChange={(f, err) => {
          setFile(f);
          setFileErr(err || "");
        }}
        error={fileErr}
      />
      <p className="text-xs text-slate-500">
        Haddii lacagtu qaldan tahay ama sawirku aan ahayn EVC/Zaad, system-ku ma aqbalayo.
      </p>

      <button
        type="submit"
        disabled={uploading}
        className="btn-primary w-full !py-3.5 text-base font-semibold"
      >
        {uploading ? "Waa la dirayaa..." : "Waxaan lacagtuuray"}
      </button>
    </form>
  );
}
