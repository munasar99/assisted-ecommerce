import { useId } from "react";

/** SVG mark — sanduuq + fallaadh (dalab & keen) */
export default function LogoMark({ className = "h-10 w-10", light = false }) {
  const uid = useId().replace(/:/g, "");
  const gradId = `logo-grad-${uid}`;
  const shadowId = `logo-shadow-${uid}`;

  return (
    <svg
      className={className}
      viewBox="0 0 48 48"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      aria-hidden
    >
      <defs>
        <linearGradient id={gradId} x1="8" y1="6" x2="42" y2="42" gradientUnits="userSpaceOnUse">
          <stop stopColor={light ? "#34d399" : "#10b981"} />
          <stop offset="0.5" stopColor={light ? "#2dd4bf" : "#059669"} />
          <stop offset="1" stopColor={light ? "#38bdf8" : "#047857"} />
        </linearGradient>
        {!light && (
          <filter id={shadowId} x="-20%" y="-20%" width="140%" height="140%">
            <feDropShadow dx="0" dy="2" stdDeviation="2" floodColor="#065f46" floodOpacity="0.25" />
          </filter>
        )}
      </defs>
      <rect
        x="4"
        y="4"
        width="40"
        height="40"
        rx="12"
        fill={`url(#${gradId})`}
        filter={light ? undefined : `url(#${shadowId})`}
      />
      <path d="M14 20h20l-2 14H16l-2-14z" fill="white" fillOpacity="0.95" />
      <path d="M14 20l10 7 10-7" stroke="white" strokeWidth="1.5" strokeLinejoin="round" fill="none" />
      <path
        d="M32 14l4 4m0 0l-4 4m4-4H28"
        stroke="white"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <circle cx="34" cy="18" r="1.5" fill="#fbbf24" />
    </svg>
  );
}
