import { BRAND } from "../constants/brand";
import LogoMark from "./LogoMark";

export default function Logo({ className = "", light = false, compact = false }) {
  return (
    <div className={`flex items-center gap-3 ${className}`}>
      <LogoMark className={compact ? "h-9 w-9" : "h-11 w-11"} light={light} />
      <div className="flex min-w-0 flex-col leading-tight">
        <span
          className={`truncate font-display text-lg font-extrabold tracking-tight ${
            light ? "text-white" : "bg-gradient-to-r from-brand-800 via-brand-700 to-brand-600 bg-clip-text text-transparent"
          }`}
        >
          {BRAND.name}
        </span>
        {!compact && (
          <span
            className={`truncate text-[11px] font-medium ${
              light ? "text-emerald-200/85" : "text-slate-500"
            }`}
          >
            {BRAND.tagline}
          </span>
        )}
      </div>
    </div>
  );
}
