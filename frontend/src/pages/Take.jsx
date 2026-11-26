import { useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { api } from "../api";
import { Difficulty, ErrorNote } from "../components.jsx";

const formatTime = (seconds) =>
  `${String(Math.floor(seconds / 60)).padStart(2, "0")}:${String(seconds % 60).padStart(2, "0")}`;

export default function Take() {
  const { id } = useParams();
  const navigate = useNavigate();

  const [test, setTest] = useState(null);
  const [error, setError] = useState("");
  const [attempt, setAttempt] = useState(null);
  const [answers, setAnswers] = useState({}); // { [questionId]: optionId }
  const [marked, setMarked] = useState({});   // { [questionId]: true }
  const [current, setCurrent] = useState(0);
  const [secondsLeft, setSecondsLeft] = useState(null);
  const [busy, setBusy] = useState(false);

  // Refs so the timer always sees the latest values.
  const answersRef = useRef(answers);
  answersRef.current = answers;
  const submittedRef = useRef(false);

  useEffect(() => {
    api.mockTest(id).then(setTest).catch((e) => setError(e.message));
  }, [id]);

  const start = async () => {
    setError("");
    setBusy(true);
    try {
      const started = await api.startAttempt(Number(id));
      setAttempt(started);
      setSecondsLeft(started.durationMinutes * 60);
    } catch (e) {
      setError(e.message);
    } finally {
      setBusy(false);
    }
  };

  const submit = async () => {
    if (submittedRef.current || !attempt) return;
    submittedRef.current = true;
    setBusy(true);
    try {
      const payload = Object.entries(answersRef.current).map(([q, o]) => ({ questionId: Number(q), optionId: Number(o) }));
      await api.submitAttempt(attempt.attemptId, payload);
      navigate(`/result/${attempt.attemptId}`, { replace: true });
    } catch (e) {
      submittedRef.current = false;
      setError(e.message);
      setBusy(false);
    }
  };
  const submitRef = useRef(submit);
  submitRef.current = submit;

  // Countdown: auto-submits when time runs out.
  useEffect(() => {
    if (!attempt) return;
    let left = attempt.durationMinutes * 60;
    const timer = setInterval(() => {
      left -= 1;
      setSecondsLeft(left);
      if (left <= 0) {
        clearInterval(timer);
        submitRef.current();
      }
    }, 1000);
    return () => clearInterval(timer);
  }, [attempt]);

  // Warn before accidentally leaving a running test.
  useEffect(() => {
    if (!attempt) return;
    const warn = (e) => {
      if (!submittedRef.current) e.preventDefault();
    };
    window.addEventListener("beforeunload", warn);
    return () => window.removeEventListener("beforeunload", warn);
  }, [attempt]);

  if (error && !test) return <ErrorNote message={error} />;
  if (!test) return <p className="muted">Loading…</p>;

  const total = test.questions.length;
  const answered = Object.keys(answers).length;

  // ------- intro screen -------
  if (!attempt) {
    return (
      <>
        <h1>{test.title}</h1>
        <p className="muted">{test.description}</p>
        <div className="card">
          <div className="stats-row">
            <div><strong>{total}</strong><span>questions</span></div>
            <div><strong>{test.durationMinutes}</strong><span>minutes</span></div>
            <div><strong>{test.passMarkPercent}%</strong><span>pass mark</span></div>
            <div><Difficulty level={test.difficulty} /><span>difficulty</span></div>
          </div>
          <ul className="rules">
            <li>The timer starts when you click Start and the test is submitted automatically when it ends.</li>
            <li>You can move between questions, change answers and mark questions for review.</li>
            <li>There is no negative marking. Unanswered questions score 0.</li>
            <li>After submitting you will see correct answers, explanations and topic-wise analysis.</li>
          </ul>
          <ErrorNote message={error} />
          <button className="btn primary big" onClick={start} disabled={busy}>
            {busy ? "Starting…" : "Start test"}
          </button>
        </div>
      </>
    );
  }

  // ------- running -------
  const question = test.questions[current];
  const status = (q) => (answers[q.id] ? "answered" : "") + (marked[q.id] ? " marked" : "");

  const confirmSubmit = () => {
    const unanswered = total - answered;
    const message = unanswered > 0 ? `You have ${unanswered} unanswered question(s). Submit anyway?` : "Submit your test?";
    if (window.confirm(message)) submit();
  };

  return (
    <div className="test-layout">
      <div className="test-main">
        <div className="sticky-bar">
          <strong>{test.title}</strong>
          <div className={`timer ${secondsLeft <= 60 ? "danger" : ""}`}>⏱ {formatTime(Math.max(secondsLeft ?? 0, 0))}</div>
        </div>

        <div className="card question">
          <p className="q-text">
            <span className="q-num">Q{current + 1}.</span>
            <span className="topic-tag">{question.topic}</span>
          </p>
          <p className="q-body">{question.text}</p>
          {question.options.map((option) => (
            <label className={`option ${answers[question.id] === option.id ? "selected" : ""}`} key={option.id}>
              <input
                type="radio"
                name={`q-${question.id}`}
                checked={answers[question.id] === option.id}
                onChange={() => setAnswers({ ...answers, [question.id]: option.id })}
              />
              {option.text}
            </label>
          ))}
        </div>

        <div className="actions between">
          <div className="actions">
            <button className="btn" disabled={current === 0} onClick={() => setCurrent(current - 1)}>← Previous</button>
            <button className="btn" disabled={current === total - 1} onClick={() => setCurrent(current + 1)}>Next →</button>
          </div>
          <div className="actions">
            <button className="btn" onClick={() => setMarked({ ...marked, [question.id]: !marked[question.id] })}>
              {marked[question.id] ? "★ Marked" : "☆ Mark for review"}
            </button>
            {answers[question.id] && (
              <button className="btn" onClick={() => { const next = { ...answers }; delete next[question.id]; setAnswers(next); }}>
                Clear answer
              </button>
            )}
          </div>
        </div>
        <ErrorNote message={error} />
      </div>

      <aside className="palette card">
        <h3>Questions</h3>
        <div className="palette-grid">
          {test.questions.map((q, index) => (
            <button key={q.id} className={`palette-btn ${status(q)} ${index === current ? "current" : ""}`} onClick={() => setCurrent(index)}>
              {index + 1}
            </button>
          ))}
        </div>
        <p className="legend"><i className="dot answered" /> answered <i className="dot marked" /> marked</p>
        <p className="meta">{answered}/{total} answered</p>
        <button className="btn primary big" onClick={confirmSubmit} disabled={busy}>
          {busy ? "Submitting…" : "Submit test"}
        </button>
      </aside>
    </div>
  );
}
