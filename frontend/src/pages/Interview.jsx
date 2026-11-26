import { useEffect, useRef, useState } from "react";
import { api } from "../api";
import { ErrorNote } from "../components.jsx";

const QUESTIONS = 6; // keep in sync with AiCoachService.InterviewQuestions

export default function Interview() {
  const [setup, setSetup] = useState({ company: "", role: "Software Engineer", type: "technical" });
  const [started, setStarted] = useState(false);
  const [history, setHistory] = useState([]); // [{ speaker: "ai" | "you", text }]
  const [answer, setAnswer] = useState("");
  const [busy, setBusy] = useState(false);
  const [finished, setFinished] = useState(false);
  const [error, setError] = useState("");
  const endRef = useRef(null);

  useEffect(() => {
    endRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [history, busy]);

  const turn = async (nextHistory) => {
    setError("");
    setBusy(true);
    try {
      const askedBefore = nextHistory.filter((t) => t.speaker === "ai").length;
      const reply = await api.interview({ ...setup, history: nextHistory });
      setHistory([...nextHistory, { speaker: "ai", text: reply.content }]);
      if (askedBefore >= QUESTIONS) setFinished(true);
    } catch (e) {
      setError(e.message);
    } finally {
      setBusy(false);
    }
  };

  const start = async (e) => {
    e.preventDefault();
    setStarted(true);
    setHistory([]);
    setFinished(false);
    await turn([]);
  };

  const send = async (e) => {
    e.preventDefault();
    const text = answer.trim();
    if (!text || busy) return;
    setAnswer("");
    const next = [...history, { speaker: "you", text }];
    setHistory(next);
    await turn(next);
  };

  if (!started) {
    return (
      <>
        <h1>AI mock interview</h1>
        <p className="muted">
          Practise answering out loud or in writing. The interviewer asks {QUESTIONS} questions, gives feedback after each
          answer, and finishes with an overall evaluation.
        </p>
        <form className="card form narrow" onSubmit={start}>
          <label>
            Company
            <input value={setup.company} onChange={(e) => setSetup({ ...setup, company: e.target.value })} required maxLength={100} placeholder="e.g. Aon, TCS, Amazon" />
          </label>
          <label>
            Role
            <input value={setup.role} onChange={(e) => setSetup({ ...setup, role: e.target.value })} required maxLength={100} />
          </label>
          <label>
            Interview type
            <select value={setup.type} onChange={(e) => setSetup({ ...setup, type: e.target.value })}>
              <option value="technical">Technical</option>
              <option value="hr">HR / behavioural</option>
            </select>
          </label>
          <button className="btn primary">Start interview</button>
        </form>
      </>
    );
  }

  const asked = history.filter((t) => t.speaker === "ai").length;

  return (
    <>
      <div className="page-head">
        <h1>{setup.company} interview</h1>
        <span className="meta">{finished ? "Finished" : `Question ${Math.min(asked, QUESTIONS)} of ${QUESTIONS}`}</span>
      </div>

      <div className="chat card">
        {history.map((t, index) => (
          <div className={`bubble ${t.speaker}`} key={index}>
            <div className="who">{t.speaker === "ai" ? "Interviewer" : "You"}</div>
            <div className="pre">{t.text}</div>
          </div>
        ))}
        {busy && <div className="bubble ai"><div className="who">Interviewer</div>Thinking…</div>}
        <div ref={endRef} />
      </div>

      <ErrorNote message={error} />

      {finished ? (
        <button className="btn primary" onClick={() => setStarted(false)}>Start a new interview</button>
      ) : (
        <form className="chat-input" onSubmit={send}>
          <textarea
            value={answer}
            onChange={(e) => setAnswer(e.target.value)}
            placeholder="Type your answer…"
            maxLength={3000}
            rows={3}
            disabled={busy || history.length === 0}
          />
          <button className="btn primary" disabled={busy || !answer.trim()}>Send</button>
        </form>
      )}
    </>
  );
}
