import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { deliveryApi, ordersApi, uploadsApi } from "../../api/endpoints";
import UploadInput from "../../components/UploadInput";
import PageShell from "../../components/PageShell";
import { BRAND } from "../../constants/brand";
import {
  DELIVERY_OPTIONS,
  DELIVERY_TYPES,
  hasDeliveryChoice,
  isHomeDelivery,
  isPickup,
  PICKUP_LABEL,
} from "../../constants/deliveryTypes";
import {
  ORDER_FORM_MAX_PRICE_USD,
  ORDER_FORM_MAX_QUANTITY,
  ORDER_FORM_MIN_PRICE_USD,
} from "../../constants/orderFormSecurity";
import { useToast } from "../../context/ToastContext";
import { useCustomerSession } from "../../context/CustomerSessionContext";
import { saveAfterOrder } from "../../utils/customerSession";
import {
  validateEmail,
  validatePhone,
  validateUrl,
} from "../../utils/validation";

export default function OrderPage() {
  const navigate = useNavigate();
  const { show } = useToast();
  const { sync } = useCustomerSession();
  const [loading, setLoading] = useState(false);
  const submittingRef = useRef(false);
  const [screenshot, setScreenshot] = useState(null);
  const [screenshotErr, setScreenshotErr] = useState("");
  const [form, setForm] = useState({
    fullName: "",
    phone: "",
    email: "",
    productName: "",
    productUrl: "",
    productPrice: "",
    quantity: "",
    deliveryType: "",
    districtId: "",
    addressDetail: "",
    notes: "",
  });
  const [errors, setErrors] = useState({});

  useEffect(() => {
    const url = sessionStorage.getItem("prefillProductUrl");
    if (url) {
      setForm((f) => ({ ...f, productUrl: url }));
      sessionStorage.removeItem("prefillProductUrl");
    }
  }, []);

  const homeDelivery = isHomeDelivery(form.deliveryType);
  const pickup = isPickup(form.deliveryType);
  const showDistrict = homeDelivery;

  const { data: zones, isLoading: zonesLoading } = useQuery({
    queryKey: ["zones"],
    queryFn: deliveryApi.active,
  });

  const selectedZone = zones?.find((z) => z.zoneId === form.districtId);

  const set = (k, v) => setForm((f) => ({ ...f, [k]: v }));

  const onDeliveryTypeChange = (deliveryType) => {
    setForm((f) => ({
      ...f,
      deliveryType,
      districtId: "",
      addressDetail:
        deliveryType === DELIVERY_TYPES.HOME ? f.addressDetail : "",
    }));
    setErrors((e) => {
      const next = { ...e };
      delete next.districtId;
      delete next.addressDetail;
      delete next.deliveryType;
      return next;
    });
  };

  const validate = () => {
    const e = {};
    if (!form.fullName.trim()) e.fullName = "Required";
    if (!validatePhone(form.phone)) e.phone = "Phone invalid (+2526...)";
    if (!validateEmail(form.email)) e.email = "Geli email sax ah";
    if (!validateUrl(form.productUrl)) e.productUrl = "Valid URL required";
    if (!hasDeliveryChoice(form.deliveryType))
      e.deliveryType = "Dooro home delivery ama tabo";
    if (homeDelivery && !form.districtId)
      e.districtId = "Dooro degmadaada (Home delivery)";
    if (homeDelivery && !form.addressDetail.trim())
      e.addressDetail = "Required";
    const qty = Number(form.quantity);
    if (!qty || qty < 1) e.quantity = "Geli tirada alaabta (ugu yaraan 1)";
    else if (qty > ORDER_FORM_MAX_QUANTITY)
      e.quantity = `Tirada ugu badan waa ${ORDER_FORM_MAX_QUANTITY} alaab`;
    const price = Number(form.productPrice);
    if (!price || price < ORDER_FORM_MIN_PRICE_USD)
      e.productPrice = "Geli qiimaha alaabta";
    else if (price > ORDER_FORM_MAX_PRICE_USD)
      e.productPrice = `Qiimaha ugu badan waa $${ORDER_FORM_MAX_PRICE_USD}`;
    setErrors(e);
    return Object.keys(e).length === 0;
  };

  const submit = async (e) => {
    e.preventDefault();
    if (submittingRef.current || loading) return;
    if (!validate()) return;
    submittingRef.current = true;
    setLoading(true);
    try {
      let orderScreenshotUrl;
      if (screenshot) {
        const fd = new FormData();
        fd.append("file", screenshot);
        const up = await uploadsApi.orderScreenshot(fd);
        orderScreenshotUrl = up.url;
      }
      const payload = {
        fullName: form.fullName,
        phone: form.phone,
        email: form.email.trim(),
        productUrl: form.productUrl,
        productName: form.productName || undefined,
        productUnitPriceUsd: Number(form.productPrice),
        quantity: Number(form.quantity),
        deliveryType: form.deliveryType,
        notes: form.notes || undefined,
        orderScreenshotUrl,
      };

      if (homeDelivery) {
        payload.districtId = form.districtId;
        payload.addressDetail = form.addressDetail;
      } else if (pickup) {
        payload.addressDetail = form.addressDetail?.trim() || undefined;
      }

      const result = await ordersApi.create(payload);

      if (result?.emailSent) {
        show("Dalabka waa la helay — email ayaa loo diray cinwaankaaga.", "success");
      } else if (result?.emailError) {
        show(`Dalabka waa la helay, laakiin email: ${result.emailError}`, "error");
      }

      saveAfterOrder({
        orderId: result.orderId,
        userId: result.userId,
        phone: form.phone.trim(),
        deliveryType: result.deliveryType,
        districtId: result.districtId,
        districtName: result.districtName,
        deliveryFee: result.deliveryFee,
      });
      sync();
      show(
        `Keydiyay: ${result.deliveryType} · ${result.districtName} (${result.districtId})`,
        "success",
      );
      navigate("/payment/pay", { replace: true });
    } catch (err) {
      show(err.message, "error");
    } finally {
      submittingRef.current = false;
      setLoading(false);
    }
  };

  return (
    <PageShell title={BRAND.name} subtitle={BRAND.tagline} centered>
      <form onSubmit={submit} className="card-elevated space-y-5 p-6 sm:p-8">
        <Field label="Magaca oo buuxa" error={errors.fullName}>
          <input
            className={inputCls}
            value={form.fullName}
            placeholder="Enter your full name"
            onChange={(e) => set("fullName", e.target.value)}
          />
        </Field>
        <Field label="Telefoon (WhatsApp)" error={errors.phone}>
          <input
            className={inputCls}
            placeholder="Enter your phone"
            value={form.phone}
            onChange={(e) => set("phone", e.target.value)}
          />
        </Field>
        <Field label="Email" error={errors.email}>
          <input
            type="email"
            className={inputCls}
            placeholder="Enter your email"
            value={form.email}
            onChange={(e) => set("email", e.target.value)}
            autoComplete="email"
          />
        </Field>
        <Field label="Magaca alaabta" error={errors.productName}>
          <input
            className={inputCls}
            placeholder="Enter product name"
            value={form.productName}
            onChange={(e) => set("productName", e.target.value)}
          />
        </Field>
        <Field label="Link alaabta (Alibaba/Amazon)" error={errors.productUrl}>
          <input
            className={inputCls}
            placeholder="product link"
            value={form.productUrl}
            onChange={(e) => set("productUrl", e.target.value)}
          />
        </Field>
        <Field
          label="Qiimaha alaabta (USD)"
          error={errors.productPrice}
          hint="Qiimaha hal alaab (USD)"
        >
          <input
            type="number"
            className={inputCls}
            placeholder="$"
            value={form.productPrice}
            onChange={(e) => set("productPrice", e.target.value)}
          />
        </Field>
        <Field
          label="Tirada alaabta"
          error={errors.quantity}
          hint="Immisa tiro alaab ayaad u baahan tahay?"
        >
          <input
            type="number"
            min={0}
            className={inputCls}
            placeholder="0"
            value={form.quantity}
            onChange={(e) => set("quantity", e.target.value)}
          />
        </Field>
        <UploadInput
          label="Sawir alaabta (optional)"
          onChange={(f, err) => {
            setScreenshot(f);
            setScreenshotErr(err || "");
          }}
          error={screenshotErr}
        />

        <Field
          label="Nooca gaarsiinta (deliveryType)"
          error={errors.deliveryType}
          hint="Marka hore dooro Home delivery ama Tabo — kadib dooro degmadaada"
        >
          <div className="space-y-2">
            {DELIVERY_OPTIONS.map((opt) => (
              <label
                key={opt.value}
                className={`flex cursor-pointer items-start gap-3 rounded-xl border p-3 transition ${
                  form.deliveryType === opt.value
                    ? "border-brand-500 bg-brand-50"
                    : "border-slate-200 hover:border-slate-300"
                }`}
              >
                <input
                  type="radio"
                  name="deliveryType"
                  className="mt-1"
                  checked={form.deliveryType === opt.value}
                  onChange={() => onDeliveryTypeChange(opt.value)}
                />
                <span className="text-sm text-slate-800">{opt.label}</span>
              </label>
            ))}
          </div>
        </Field>

        {pickup && (
          <p className="rounded-lg border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-700">
            <strong>{PICKUP_LABEL}:</strong> degmo ma muuqato — lacag delivery
            $0.
          </p>
        )}

        {showDistrict && (
          <>
            <Field
              label="Degmadaada (Home delivery)"
              error={errors.districtId}
              hint="Dooro degmada gurigaagu ku yaal"
            >
              <select
                className={inputCls}
                value={form.districtId}
                disabled={zonesLoading}
                onChange={(e) => set("districtId", e.target.value)}
              >
                <option value="">
                  {zonesLoading
                    ? "Degmooyinka waa la soo dejinayaa..."
                    : "— Dooro degmadaada —"}
                </option>
                {(zones ?? []).map((z) => (
                  <option key={z.zoneId} value={z.zoneId}>
                    {z.districtName} ({z.zoneId}) — ${z.feeUsd} delivery
                  </option>
                ))}
              </select>
              {!zonesLoading && (zones ?? []).length === 0 && (
                <p className="mt-1 text-sm text-amber-700">
                  Degmo lama helin. Hubi in backend uu socdo (dotnet run).
                </p>
              )}
            </Field>

            {selectedZone && (
              <p className="rounded-lg border border-brand-200 bg-brand-50 px-3 py-2 text-sm font-medium text-brand-800">
                Lacag delivery: ${selectedZone.feeUsd}
              </p>
            )}
          </>
        )}

        {homeDelivery && (
          <Field label="Cinwaanka guriga" error={errors.addressDetail}>
            <textarea
              className={inputCls}
              rows={2}
              placeholder="Geli cinwaanka gurigaaga"
              value={form.addressDetail}
              onChange={(e) => set("addressDetail", e.target.value)}
            />
          </Field>
        )}

        {pickup && (
          <Field label="Faallo tabo (optional)">
            <input
              className={inputCls}
              placeholder="Tusaale: goobta aad ka soo qaadanayso"
              value={form.addressDetail}
              onChange={(e) => set("addressDetail", e.target.value)}
            />
          </Field>
        )}

        <Field label="Faallo">
          <textarea
            className={inputCls}
            rows={2}
            placeholder="Enter any additional notes"
            value={form.notes}
            onChange={(e) => set("notes", e.target.value)}
          />
        </Field>
        <button
          type="submit"
          disabled={loading}
          className="btn-primary w-full !py-3.5 text-base font-semibold"
        >
          {loading ? "Waa la dirayaa..." : "Send"}
        </button>
      </form>
    </PageShell>
  );
}

const inputCls = "input-field";

function Field({ label, error, hint, children }) {
  return (
    <div>
      <label className="mb-1 block text-sm font-medium text-slate-700">
        {label}
      </label>
      {hint && <p className="mb-1 text-xs text-slate-500">{hint}</p>}
      {children}
      {error && <p className="mt-1 text-sm text-red-600">{error}</p>}
    </div>
  );
}
