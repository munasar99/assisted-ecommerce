import { useEffect, useRef, useState } from "react";
import { chatApi } from "../api/endpoints";

const SESSION_KEY = "aiChatSessionId";

function newSessionId() {
  return `sess-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
}

export default function AiChatWidget() {
  const [open, setOpen] = useState(false);
  const [messages, setMessages] = useState([
    { role: "assistant", text: "Salaan! Sideen kuu caawin karaa? Su'aal dalab, lacag bixinta, ama delivery." },
  ]);
  const [input, setInput] = useState("");
  const [loading, setLoading] = useState(false);
  const [sessionId] = useState(() => sessionStorage.getItem(SESSION_KEY) || newSessionId());
  const bottomRef = useRef(null);

  useEffect(() => {
    sessionStorage.setItem(SESSION_KEY, sessionId);
  }, [sessionId]);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages, open]);

  const send = async (e) => {
    e.preventDefault();
    const text = input.trim();
    if (!text || loading) return;
    setInput("");
    setMessages((m) => [...m, { role: "user", text }]);
    setLoading(true);
    try {
      const res = await chatApi.message({ message: text, sessionId });
      let reply = res?.reply ?? "Jawaab lama helin.";
      try {
        const parsed = JSON.parse(reply);
        if (parsed.message) reply = parsed.message;
      } catch {
        /* plain text */
      }
      setMessages((m) => [...m, { role: "assistant", text: reply }]);
    } catch (err) {
      setMessages((m) => [
        ...m,
        { role: "assistant", text: err.message || "Chat ma shaqeynayo. Hubi Node.js + API keys." },
      ]);
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <button
        type="button"
        className="ai-chat-fab"
        aria-label="AI Chat"
        onClick={() => setOpen((o) => !o)}
      >
        {open ? "✕" : "💬 AI"}
      </button>

      {open && (
        <div className="ai-chat-panel">
          <div className="ai-chat-header">
            <strong>Caawiye AI</strong>
            <span className="text-xs text-slate-500">Af-Soomaali</span>
          </div>
          <div className="ai-chat-messages">
            {messages.map((m, i) => (
              <div key={i} className={`ai-chat-bubble ${m.role === "user" ? "is-user" : "is-bot"}`}>
                {m.text}
              </div>
            ))}
            {loading && <div className="ai-chat-bubble is-bot text-slate-400">Waa la qorayaa...</div>}
            <div ref={bottomRef} />
          </div>
          <form onSubmit={send} className="ai-chat-input-row">
            <input
              className="input-field !py-2"
              placeholder="Qor su'aashaada..."
              value={input}
              onChange={(e) => setInput(e.target.value)}
              disabled={loading}
            />
            <button type="submit" className="btn-primary !px-4 !py-2" disabled={loading || !input.trim()}>
              Dir
            </button>
          </form>
        </div>
      )}
    </>
  );
}
