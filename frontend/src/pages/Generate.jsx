import { useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { api } from "../api";
import { ErrorNote } from "../components.jsx";

const IDEAS = ["SQL joins and subqueries", "Operating systems", "OOP concepts", "Percentages and profit/loss", "Data structures output questions"];

export default function Generate() {
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const [companies, setCompanies] = useState([]);
  const [form, setForm] = useState({
    companySlug: params.get("company") ?? "",
    topic: "",
    difficulty: "medium",
    numberOfQuestions: 15,
    focus: "mixed",
  });
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    api.companies().then(setCompanies).catch(() => {});
  }, []);

  const set = (field) => (e) => setForm({ ...form, [field]: e.target.value });

  const submit = async (e) => {
    e.preventDefault();
    setError("");
    setBusy(true);
    try {
      const paper = await api.generatePaper({
        ...form,
        companySlug: form.companySlug || null,
        topic: form.topic.trim() || null,
        numberOfQuestions: Number(form.numberOfQuestions),
      });
      navigate(`/test/${paper.id}`);
    } catch (err) {
      setError(err.message);
      setBusy(false);
    }
  };

  return (
    <>
      <h1>Generate a paper with AI</h1>
      <p className="muted">
        Choose a company, a topic, or both. The AI writes a fresh set of questions with explanations. It can
        make mistakes, so use the explanations to double-check anything surprising.
      </p>

      <form className="card form" onSubmit={submit}>
        <div className="row">
          <label>
            Company (optional)
            <select value={form.companySlug} onChange={set("companySlug")}>
              <option value="">Any / none</option>
              {companies.map((c) => <option key={c.slug} value={c.slug}>{c.name}</option>)}
            </select>
          </label>
          <label>
            Topic (optional if a company is chosen)
            <input value={form.topic} onChange={set("topic")} maxLength={150} placeholder="e.g. SQL joins" />
          </label>
        </div>
        <div className="chips">
          {IDEAS.map((idea) => (
            <button type="button" className="chip" key={idea} onClick={() => setForm({ ...form, topic: idea })}>{idea}</button>
          ))}
        </div>

        <div className="row">
          <label>
            Difficulty
            <select value={form.difficulty} onChange={set("difficulty")}>
              <option value="easy">Easy</option>
              <option value="medium">Medium</option>
              <option value="hard">Hard</option>
            </select>
          </label>
          <label>
            Focus
            <select value={form.focus} onChange={set("focus")}>
              <option value="mixed">Mixed</option>
              <option value="aptitude">Aptitude</option>
              <option value="technical">Technical</option>
            </select>
          </label>
          <label>
            Questions (5-30)
            <input type="number" min={5} max={30} value={form.numberOfQuestions} onChange={set("numberOfQuestions")} required />
          </label>
        </div>

        <ErrorNote message={error} />
        <button className="btn primary" disabled={busy}>
          {busy ? "Generating… this can take up to 40 seconds" : "✨ Generate"}
        </button>
        <p className="hint">Limit: 5 AI papers per day per account. 15 AI requests per minute.</p>
      </form>
    </>
  );
}
