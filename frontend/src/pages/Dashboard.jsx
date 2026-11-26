import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { api } from "../api";
import { Bar, ErrorNote } from "../components.jsx";

export default function Dashboard() {
  const [data, setData] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    api.dashboard().then(setData).catch((e) => setError(e.message));
  }, []);

  if (error) return <ErrorNote message={error} />;
  if (!data) return <p className="muted">Loading…</p>;

  const d = data.solvedByDifficulty;
  const empty = data.mockTestsTaken === 0 && data.problemsSolved === 0;

  return (
    <>
      <h1>Hi {data.fullName.split(" ")[0]} 👋</h1>
      {empty && (
        <div className="card">
          <p>Welcome! Start with a mock paper or a DSA problem and your progress will show up here.</p>
          <div className="actions">
            <Link className="btn primary" to="/companies">Take a mock test</Link>
            <Link className="btn" to="/practice">Solve a problem</Link>
          </div>
        </div>
      )}

      <div className="grid stats">
        <div className="card stat"><strong>{data.streakDays}🔥</strong><span>day streak</span></div>
        <div className="card stat"><strong>{data.mockTestsTaken}</strong><span>mock tests taken</span></div>
        <div className="card stat"><strong>{data.averagePercent}%</strong><span>average score</span></div>
        <div className="card stat"><strong>{data.bestPercent}%</strong><span>best score</span></div>
        <div className="card stat"><strong>{data.problemsSolved}/{data.totalProblems}</strong><span>problems solved</span></div>
      </div>

      <div className="two-col">
        <div>
          <h2>DSA progress</h2>
          <div className="card">
            {[["Easy", d.easy, d.easyTotal], ["Medium", d.medium, d.mediumTotal], ["Hard", d.hard, d.hardTotal]].map(([label, solved, total]) => (
              <div className="topic-row" key={label}>
                <div className="topic-name">{label}</div>
                <Bar percent={total ? (solved / total) * 100 : 0} tone="good" />
                <div className="topic-score">{solved}/{total}</div>
              </div>
            ))}
            <Link className="btn small" to="/practice">Practice more</Link>
          </div>
        </div>

        <div>
          <h2>Topic accuracy</h2>
          <div className="card">
            {data.topics.length === 0 && <p className="muted">Take a mock test to see your strengths and weak spots.</p>}
            {data.topics.map((t) => (
              <div className="topic-row" key={t.topic}>
                <div className="topic-name">{t.topic}</div>
                <Bar percent={t.percent} />
                <div className="topic-score">{t.percent}%</div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {data.weakTopics.length > 0 && (
        <div className="card weak">
          <strong>Focus on:</strong> {data.weakTopics.join(", ")}.{" "}
          <Link to="/generate">Generate a practice quiz →</Link>
        </div>
      )}

      <h2>Recent tests</h2>
      <div className="stack">
        {data.recent.length === 0 && <p className="muted">No tests yet.</p>}
        {data.recent.map((a) => (
          <Link to={`/result/${a.attemptId}`} className="card row-card" key={a.attemptId}>
            <div>
              <strong>{a.title}</strong>
              <div className="meta">{new Date(a.submittedAtUtc).toLocaleString()}</div>
            </div>
            <div className={`score-pill ${a.passed ? "pass" : "fail"}`}>{a.percent}%</div>
          </Link>
        ))}
      </div>
    </>
  );
}
