import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { api } from "../api";
import { Bar, ErrorNote } from "../components.jsx";

export default function Result() {
  const { attemptId } = useParams();
  const [result, setResult] = useState(null);
  const [error, setError] = useState("");
  const [filter, setFilter] = useState("all");

  useEffect(() => {
    api.attemptResult(attemptId).then(setResult).catch((e) => setError(e.message));
  }, [attemptId]);

  if (error) return <ErrorNote message={error} />;
  if (!result) return <p className="muted">Loading…</p>;

  const correct = result.review.filter((r) => r.isCorrect).length;
  const skipped = result.review.filter((r) => !r.answered).length;
  const wrong = result.review.length - correct - skipped;

  const visible = result.review.filter((r) => {
    if (filter === "wrong") return r.answered && !r.isCorrect;
    if (filter === "skipped") return !r.answered;
    return true;
  });

  return (
    <>
      <div className={`card result ${result.passed ? "pass" : "fail"}`}>
        <div className="verdict">{result.passed ? "🎉 Passed" : "Keep practising"}</div>
        <h2 className="result-title">{result.title}</h2>
        <div className="big-score">{result.percent}%</div>
        <p className="muted">
          {result.score}/{result.maxScore} points · {correct} correct · {wrong} wrong · {skipped} skipped
        </p>
        <div className="actions center">
          <Link className="btn primary" to={`/test/${result.mockTestId}`}>Retake</Link>
          <Link className="btn" to="/generate">New AI paper</Link>
          <Link className="btn" to="/dashboard">Dashboard</Link>
        </div>
      </div>

      <h2>Topic-wise analysis</h2>
      <div className="card">
        {result.topics.map((t) => (
          <div className="topic-row" key={t.topic}>
            <div className="topic-name">{t.topic}</div>
            <Bar percent={t.percent} />
            <div className="topic-score">{t.correct}/{t.total}</div>
          </div>
        ))}
      </div>

      <div className="page-head">
        <h2>Review</h2>
        <div className="segmented">
          {[["all", "All"], ["wrong", "Wrong"], ["skipped", "Skipped"]].map(([value, label]) => (
            <button key={value} className={filter === value ? "active" : ""} onClick={() => setFilter(value)}>{label}</button>
          ))}
        </div>
      </div>

      {visible.length === 0 && <p className="muted">Nothing here. 👏</p>}
      {visible.map((r) => {
        const number = result.review.indexOf(r) + 1;
        const state = !r.answered ? "skipped" : r.isCorrect ? "correct" : "wrong";
        return (
          <div className={`card review ${state}`} key={r.questionId}>
            <p className="q-text">
              <span className="q-num">Q{number}.</span>
              <span className="topic-tag">{r.topic}</span>
            </p>
            <p className="q-body">{r.text}</p>
            {r.options.map((o) => (
              <div className={`review-option ${o.isCorrect ? "is-correct" : ""} ${o.isSelected && !o.isCorrect ? "is-wrong" : ""}`} key={o.id}>
                {o.isCorrect ? "✅" : o.isSelected ? "❌" : "◻️"} {o.text}
                {o.isSelected && <span className="you"> (your answer)</span>}
              </div>
            ))}
            {!r.answered && <p className="meta">You skipped this question.</p>}
            {r.explanation && <p className="explanation"><strong>Explanation:</strong> {r.explanation}</p>}
          </div>
        );
      })}
    </>
  );
}
