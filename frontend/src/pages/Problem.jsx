import { useEffect, useRef, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { api } from "../api";
import { useAuth } from "../auth.jsx";
import { runTests } from "../runner";
import { Difficulty, ErrorNote, RichText } from "../components.jsx";

const LANGUAGES = [
  { id: "javascript", label: "JavaScript (runs here)" },
  { id: "python", label: "Python (AI review)" },
  { id: "java", label: "Java (AI review)" },
  { id: "cpp", label: "C++ (AI review)" },
];

const STUB = {
  python: "# Write your solution here\n",
  java: "// Write your solution here\n",
  cpp: "// Write your solution here\n",
};

export default function Problem() {
  const { slug } = useParams();
  const { user } = useAuth();

  const [problem, setProblem] = useState(null);
  const [error, setError] = useState("");
  const [language, setLanguage] = useState("javascript");
  const [codes, setCodes] = useState({}); // one editor buffer per language
  const [tab, setTab] = useState("description");
  const [run, setRun] = useState(null);   // { results } or { error }
  const [running, setRunning] = useState(false);
  const [message, setMessage] = useState(null); // { type, text }
  const [ai, setAi] = useState(null);     // { mode, content }
  const [aiBusy, setAiBusy] = useState("");
  const [hintLevel, setHintLevel] = useState(0);
  const [history, setHistory] = useState([]);
  const editorRef = useRef(null);

  const code = codes[language] ?? (language === "javascript" ? problem?.starterCode : STUB[language]) ?? "";
  const setCode = (value) => setCodes({ ...codes, [language]: value });

  useEffect(() => {
    setProblem(null);
    setRun(null);
    setAi(null);
    setMessage(null);
    setHintLevel(0);
    setCodes({});
    api.problem(slug).then(setProblem).catch((e) => setError(e.message));
  }, [slug]);

  // Restore the latest saved JavaScript solution, if any.
  useEffect(() => {
    if (!problem || !user) return;
    api.submissions(problem.id).then((list) => {
      setHistory(list);
      const latest = list.find((s) => s.language === "javascript");
      if (latest) setCodes((current) => ({ javascript: latest.code, ...current }));
    }).catch(() => {});
  }, [problem?.id, user?.id]);

  const onKeyDown = (e) => {
    // Tab inserts two spaces instead of leaving the editor.
    if (e.key !== "Tab") return;
    e.preventDefault();
    const el = e.target;
    const { selectionStart: start, selectionEnd: end } = el;
    setCode(code.slice(0, start) + "  " + code.slice(end));
    requestAnimationFrame(() => el.setSelectionRange(start + 2, start + 2));
  };

  const execute = async () => {
    setRunning(true);
    setMessage(null);
    const outcome = await runTests(code, problem.functionName, problem.testCases);
    setRun(outcome);
    setRunning(false);
    return outcome;
  };

  const submit = async () => {
    if (!user) return setMessage({ type: "error", text: "Log in to submit and track your progress." });
    setMessage(null);
    try {
      if (language === "javascript") {
        const outcome = await execute();
        if (outcome.error) return;
        const passed = outcome.results.filter((r) => r.ok).length;
        const result = await api.submitSolution(problem.id, {
          language, code, passedTests: passed, totalTests: problem.testCases.length,
        });
        if (result.passed) {
          setProblem({ ...problem, solved: true });
          setMessage({ type: "success", text: result.firstSolve ? "🎉 Solved! All tests passed. Full explanation unlocked." : "✅ All tests passed again." });
        } else {
          setMessage({ type: "error", text: `${passed}/${problem.testCases.length} tests passed. Keep going!` });
        }
      } else {
        const result = await api.submitSolution(problem.id, { language, code, selfReported: true });
        setProblem({ ...problem, solved: result.passed || problem.solved });
        setMessage({ type: "success", text: "Marked as solved (self-reported, not machine-verified)." });
      }
      api.submissions(problem.id).then(setHistory).catch(() => {});
    } catch (e) {
      setMessage({ type: "error", text: e.message });
    }
  };

  const askAi = async (mode, level) => {
    if (!user) return setMessage({ type: "error", text: "Log in to use the AI mentor." });
    setAiBusy(mode);
    setMessage(null);
    try {
      const reply = await api.coach(problem.id, { mode, language, code, hintLevel: level ?? 1 });
      setAi(reply);
    } catch (e) {
      setMessage({ type: "error", text: e.message });
    } finally {
      setAiBusy("");
    }
  };

  const nextHint = () => {
    const level = Math.min(hintLevel + 1, 3);
    setHintLevel(level);
    askAi("hint", level);
  };

  if (error && !problem) return <ErrorNote message={error} />;
  if (!problem) return <p className="muted">Loading…</p>;

  const passedCount = run?.results?.filter((r) => r.ok).length ?? 0;

  return (
    <div className="problem-layout">
      <section className="problem-left">
        <Link to="/practice" className="meta">← All problems</Link>
        <h1>{problem.solved && "✅ "}{problem.title}</h1>
        <div className="tags">
          <Difficulty level={problem.difficulty} />
          {problem.topics.map((t) => <span className="tag" key={t}>{t}</span>)}
          {problem.companies.map((c) => <span className="tag company" key={c}>{c}</span>)}
        </div>

        <div className="segmented small">
          <button className={tab === "description" ? "active" : ""} onClick={() => setTab("description")}>Description</button>
          <button className={tab === "submissions" ? "active" : ""} onClick={() => setTab("submissions")}>My submissions</button>
        </div>

        {tab === "description" ? (
          <div className="card statement">
            <p className="pre">{problem.description}</p>
            <h4>Examples</h4>
            <pre><code>{problem.examples}</code></pre>
            <h4>Constraints</h4>
            <pre><code>{problem.constraints}</code></pre>
          </div>
        ) : (
          <div className="card">
            {!user && <p className="muted">Log in to see your submissions.</p>}
            {user && history.length === 0 && <p className="muted">No submissions yet.</p>}
            {history.map((s) => (
              <div className="submission" key={s.id}>
                <span>{s.passed ? "✅" : "❌"} {s.language}{s.verified ? " (verified)" : s.passed ? " (self-reported)" : ""}</span>
                <span className="meta">{s.passedTests}/{s.totalTests} · {new Date(s.submittedAtUtc).toLocaleString()}</span>
                <button className="btn small" onClick={() => { setLanguage(s.language); setCodes({ ...codes, [s.language]: s.code }); }}>Load</button>
              </div>
            ))}
          </div>
        )}
      </section>

      <section className="problem-right">
        <div className="editor-bar">
          <select value={language} onChange={(e) => setLanguage(e.target.value)}>
            {LANGUAGES.map((l) => <option key={l.id} value={l.id}>{l.label}</option>)}
          </select>
          {language === "javascript" && (
            <button className="btn small" onClick={() => setCodes({ ...codes, javascript: problem.starterCode })}>Reset</button>
          )}
        </div>

        <textarea
          ref={editorRef}
          className="editor"
          spellCheck={false}
          value={code}
          onChange={(e) => setCode(e.target.value)}
          onKeyDown={onKeyDown}
        />

        <div className="actions">
          {language === "javascript" && (
            <button className="btn" onClick={execute} disabled={running}>{running ? "Running…" : "▶ Run tests"}</button>
          )}
          <button className="btn primary" onClick={submit} disabled={running}>
            {language === "javascript" ? "Submit" : "Mark as solved"}
          </button>
        </div>

        {message && <p className={`alert ${message.type}`}>{message.text}</p>}

        {run?.error && <p className="alert error pre">{run.error}</p>}
        {run?.results && (
          <div className="card tests">
            <strong>{passedCount}/{run.results.length} tests passed</strong>
            {run.results.map((r, index) => {
              const testCase = problem.testCases[index];
              return (
                <div className={`test-case ${r.ok ? "ok" : "fail"}`} key={index}>
                  <div>{r.ok ? "✅" : "❌"} Test {index + 1}</div>
                  <code>input: {JSON.stringify(testCase.input)}</code>
                  {!r.ok && (
                    <>
                      <code>expected: {JSON.stringify(testCase.expected)}</code>
                      <code>got: {r.error ? r.error : r.got}</code>
                    </>
                  )}
                </div>
              );
            })}
          </div>
        )}

        <div className="card">
          <strong>🤖 AI mentor</strong>
          <div className="actions">
            <button className="btn small" onClick={nextHint} disabled={Boolean(aiBusy)}>
              {aiBusy === "hint" ? "Thinking…" : hintLevel === 0 ? "💡 Hint" : hintLevel < 3 ? `💡 Next hint (${hintLevel}/3)` : "💡 Hint (max level)"}
            </button>
            <button className="btn small" onClick={() => askAi("review")} disabled={Boolean(aiBusy)}>
              {aiBusy === "review" ? "Reviewing…" : "🔍 Review my code"}
            </button>
            <button className="btn small" onClick={() => askAi("explain")} disabled={Boolean(aiBusy)} title={problem.solved ? "" : "Solve the problem to unlock"}>
              {problem.solved ? "📘 Explain solution" : "🔒 Explain solution"}
            </button>
          </div>
          {ai && <RichText text={ai.content} />}
          <p className="hint">AI can make mistakes. Treat hints and reviews as guidance, not ground truth.</p>
        </div>
      </section>
    </div>
  );
}
