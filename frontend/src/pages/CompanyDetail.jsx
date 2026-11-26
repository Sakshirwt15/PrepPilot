import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { api } from "../api";
import { useAuth } from "../auth.jsx";
import { Difficulty, ErrorNote } from "../components.jsx";

export default function CompanyDetail() {
  const { slug } = useParams();
  const { user } = useAuth();
  const navigate = useNavigate();
  const [company, setCompany] = useState(null);
  const [error, setError] = useState("");
  const [ai, setAi] = useState({ difficulty: "medium", numberOfQuestions: 15, focus: "mixed" });
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    setCompany(null);
    api.company(slug).then(setCompany).catch((e) => setError(e.message));
  }, [slug]);

  const generate = async () => {
    if (!user) {
      navigate("/login", { state: { from: `/companies/${slug}` } });
      return;
    }
    setError("");
    setBusy(true);
    try {
      const paper = await api.generatePaper({ companySlug: slug, ...ai, numberOfQuestions: Number(ai.numberOfQuestions) });
      navigate(`/test/${paper.id}`);
    } catch (e) {
      setError(e.message);
      setBusy(false);
    }
  };

  if (error && !company) return <ErrorNote message={error} />;
  if (!company) return <p className="muted">Loading…</p>;

  return (
    <>
      <div className="page-head">
        <h1><span className="emoji">{company.emoji}</span> {company.name}</h1>
        <Link className="btn" to={`/practice?company=${encodeURIComponent(company.name)}`}>DSA problems for {company.name}</Link>
      </div>
      <p className="muted">{company.description}</p>

      <div className="card">
        <h3>Typical hiring pattern</h3>
        <ol className="pattern">
          {company.pattern.map((line) => <li key={line}>{line}</li>)}
        </ol>
      </div>

      <h2>Mock papers</h2>
      <div className="stack">
        {company.papers.map((paper) => (
          <div className="card row-card" key={paper.id}>
            <div>
              <strong>{paper.title}</strong> {paper.isAiGenerated && <span className="badge ai">AI</span>}
              <div className="meta">
                <Difficulty level={paper.difficulty} /> · {paper.questionCount} questions · {paper.durationMinutes} min · pass mark {paper.passMarkPercent}%
              </div>
            </div>
            <Link className="btn primary" to={`/test/${paper.id}`}>Start</Link>
          </div>
        ))}
      </div>

      <h2>Need more practice?</h2>
      <div className="card form">
        <p className="muted">Generate a brand-new {company.name}-style paper with AI.</p>
        <div className="row">
          <label>
            Difficulty
            <select value={ai.difficulty} onChange={(e) => setAi({ ...ai, difficulty: e.target.value })}>
              <option value="easy">Easy</option>
              <option value="medium">Medium</option>
              <option value="hard">Hard</option>
            </select>
          </label>
          <label>
            Focus
            <select value={ai.focus} onChange={(e) => setAi({ ...ai, focus: e.target.value })}>
              <option value="mixed">Mixed</option>
              <option value="aptitude">Aptitude</option>
              <option value="technical">Technical</option>
            </select>
          </label>
          <label>
            Questions (5-30)
            <input type="number" min={5} max={30} value={ai.numberOfQuestions} onChange={(e) => setAi({ ...ai, numberOfQuestions: e.target.value })} />
          </label>
        </div>
        <ErrorNote message={error} />
        <button className="btn primary" onClick={generate} disabled={busy}>
          {busy ? "Generating… this can take up to 40 seconds" : "✨ Generate AI paper"}
        </button>
        <p className="hint">Limit: 5 AI papers per day per account.</p>
      </div>
    </>
  );
}
