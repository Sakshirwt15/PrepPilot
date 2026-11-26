import { useEffect, useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { api } from "../api";
import { Difficulty, ErrorNote } from "../components.jsx";

export default function Practice() {
  const [params, setParams] = useSearchParams();
  const [all, setAll] = useState(null);
  const [error, setError] = useState("");

  const difficulty = params.get("difficulty") ?? "";
  const topic = params.get("topic") ?? "";
  const company = params.get("company") ?? "";
  const search = params.get("search") ?? "";

  // Filters are applied by the API. A separate unfiltered list feeds the dropdown options.
  const [problems, setProblems] = useState(null);
  useEffect(() => {
    api.problems().then(setAll).catch((e) => setError(e.message));
  }, []);
  useEffect(() => {
    api.problems({ difficulty, topic, company, search }).then(setProblems).catch((e) => setError(e.message));
  }, [difficulty, topic, company, search]);

  const topics = useMemo(() => [...new Set((all ?? []).flatMap((p) => p.topics))].sort(), [all]);
  const companies = useMemo(() => [...new Set((all ?? []).flatMap((p) => p.companies))].sort(), [all]);

  const setFilter = (key) => (e) => {
    const next = new URLSearchParams(params);
    if (e.target.value) next.set(key, e.target.value);
    else next.delete(key);
    setParams(next, { replace: true });
  };

  const solvedCount = (all ?? []).filter((p) => p.solved).length;

  return (
    <>
      <div className="page-head">
        <h1>DSA practice</h1>
        {all && <span className="meta">{solvedCount}/{all.length} solved</span>}
      </div>
      <p className="muted">
        Solve in JavaScript and run your code against test cases right in the browser. Company tags show where a
        problem is commonly reported; this is not an official list.
      </p>

      <div className="filters">
        <input placeholder="Search problems…" value={search} onChange={setFilter("search")} />
        <select value={difficulty} onChange={setFilter("difficulty")}>
          <option value="">All difficulties</option>
          <option value="easy">Easy</option>
          <option value="medium">Medium</option>
          <option value="hard">Hard</option>
        </select>
        <select value={topic} onChange={setFilter("topic")}>
          <option value="">All topics</option>
          {topics.map((t) => <option key={t}>{t}</option>)}
        </select>
        <select value={company} onChange={setFilter("company")}>
          <option value="">All companies</option>
          {companies.map((c) => <option key={c}>{c}</option>)}
        </select>
      </div>

      <ErrorNote message={error} />
      {!problems && !error && <p className="muted">Loading…</p>}
      {problems?.length === 0 && <p className="muted">No problems match these filters.</p>}

      <div className="stack">
        {problems?.map((p) => (
          <Link to={`/practice/${p.slug}`} className="card row-card problem-row" key={p.id}>
            <div>
              <span className="solved-mark">{p.solved ? "✅" : "⬜"}</span> <strong>{p.title}</strong>
              <div className="tags">
                {p.topics.map((t) => <span className="tag" key={t}>{t}</span>)}
              </div>
            </div>
            <div className="right">
              <Difficulty level={p.difficulty} />
              <div className="tags">{p.companies.slice(0, 3).map((c) => <span className="tag company" key={c}>{c}</span>)}</div>
            </div>
          </Link>
        ))}
      </div>
    </>
  );
}
