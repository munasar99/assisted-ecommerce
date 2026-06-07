/** Icon cad oo xirfad leh — AI caawiye */
export default function AiAssistantIcon({ className = "h-7 w-7" }) {
  return (
    <svg
      className={className}
      viewBox="0 0 24 24"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      aria-hidden
    >
      <path
        d="M12 2.25l.95 2.92h3.08l-2.5 1.82.95 2.92L12 8.09 9.52 9.91l.95-2.92-2.5-1.82h3.08L12 2.25z"
        fill="currentColor"
      />
      <path
        d="M5.5 13.5a6.5 6.5 0 0113 0v2a2 2 0 01-2 2h-9a2 2 0 01-2-2v-2z"
        stroke="currentColor"
        strokeWidth="1.6"
        strokeLinejoin="round"
      />
      <circle cx="9.25" cy="14.25" r="0.85" fill="currentColor" />
      <circle cx="14.75" cy="14.25" r="0.85" fill="currentColor" />
      <path
        d="M8.5 19h7"
        stroke="currentColor"
        strokeWidth="1.6"
        strokeLinecap="round"
      />
      <path
        d="M18.5 4.5l.6 1.85 1.9.55-1.55 1.12.6 1.85-1.55-1.12-1.55 1.12.6-1.85-1.55-1.12 1.9-.55.6-1.85z"
        fill="currentColor"
        opacity="0.85"
      />
    </svg>
  );
}

export function AiCloseIcon({ className = "h-6 w-6" }) {
  return (
    <svg
      className={className}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      aria-hidden
    >
      <path d="M6 6l12 12M18 6L6 18" />
    </svg>
  );
}
