import { useCallback, useEffect, useRef, useState } from "react";
import { useLocation } from "react-router-dom";
import { chatApi } from "../api/endpoints";
import AiAssistantIcon from "./AiAssistantIcon";

const SESSION_KEY = "aiChatSessionId";

const QUICK_QUESTIONS = [
  "Sidee delivery u sameeyaa?",
  "Sidee lacag u bixiyaa?",
  "Sideen dalabka u raadiyaa?",
  "Sidee order u sameeyaa?",
];

const WELCOME =
  "Salaan! Waxaan si toos ah kuu caawin karaa delivery, lacag bixinta, raadinta dalabka, iyo sida order loo sameeyo.\n\nRiix su'aal hoose ama qor su'aashaada.";

function newSessionId() {
  return `sess-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
}

function cleanReply(text) {
  let reply = text ?? "Jawaab lama helin.";
  try {
    const parsed = JSON.parse(reply);
    if (parsed.message) reply = parsed.message;
  } catch {
    /* plain text */
  }
  return reply.replace(/\*\*(.+?)\*\*/g, "$1");
}

export default function AiChatWidget() {
  const { pathname } = useLocation();
  const [open, setOpen] = useState(false);
  const [messages, setMessages] = useState([{ role: "assistant", text: WELCOME }]);
  const [input, setInput] = useState("");
  const [loading, setLoading] = useState(false);
  const [sessionId] = useState(() => sessionStorage.getItem(SESSION_KEY) || newSessionId());
  const bottomRef = useRef(null);

  useEffect(() => {
    sessionStorage.setItem(SESSION_KEY, sessionId);
  }, [sessionId]);

  // Bog kale → chat is xir
  useEffect(() => {
    setOpen(false);
  }, [pathname]);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages, open, loading]);

  const askQuestion = useCallback(
    async (text) => {
      const q = text?.trim();
      if (!q || loading) return;
      setInput("");
      setMessages((m) => [...m, { role: "user", text: q }]);
      setLoading(true);
      try {
        const res = await chatApi.message({ message: q, sessionId });
        const reply = cleanReply(res?.reply);
        setMessages((m) => [...m, { role: "assistant", text: reply }]);
      } catch (err) {
        setMessages((m) => [
          ...m,
          {
            role: "assistant",
            text:
              err.message ||
              "Chat ma shaqeynayo. Hubi in Node.js uu socdo (port 3001) iyo ASP.NET (5298).",
          },
        ]);
      } finally {
        setLoading(false);
      }
    },
    [loading, sessionId],
  );

  const send = (e) => {
    e.preventDefault();
    askQuestion(input);
  };

  const showQuick = messages.length <= 2 && !loading;

  return (
    <>
      <button
        type="button"
        className={`ai-chat-fab group ${open ? "ai-chat-fab-active" : ""}`}
        aria-label="Caawiye AI"
        aria-expanded={open}
        title="Caawiye — weydii su'aal"
        onClick={() => setOpen(true)}
      >
        <AiAssistantIcon />
      </button>

      {open && (
        <div className="ai-chat-panel">
          <div className="ai-chat-header">
            <strong>Caawiye</strong>
            <span className="text-xs text-slate-500">Jawaab toos ah</span>
          </div>
          <div className="ai-chat-messages">
            {messages.map((m, i) => (
              <div key={i} className={`ai-chat-bubble ${m.role === "user" ? "is-user" : "is-bot"}`}>
                {m.text}
              </div>
            ))}
            {showQuick && (
              <div className="ai-chat-quick">
                {QUICK_QUESTIONS.map((q) => (
                  <button
                    key={q}
                    type="button"
                    className="ai-chat-quick-btn"
                    disabled={loading}
                    onClick={() => askQuestion(q)}
                  >
                    {q}
                  </button>
                ))}
              </div>
            )}
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
