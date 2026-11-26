import { useEffect, useState } from "react";
import { api } from "../api";
import { ErrorNote } from "../components.jsx";

export default function Leaderboard() {
  const [rows, setRows] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    api.leaderboard().then(setRows).catch((e) => setError(e.message));
  }, []);

  if (error) return <ErrorNote message={error} />;
  if (!rows) return <p className="muted">Loading…</p>;

  return (
    <>
      <h1>Leaderboard</h1>
      <p className="muted">10 points per solved problem and 5 points per completed mock test. Names are shortened for privacy.</p>
      {rows.length === 0 && <p className="muted">Nobody has scored yet. Be the first!</p>}
      {rows.length > 0 && (
        <div className="card table-wrap">
          <table>
            <thead>
              <tr><th>#</th><th>Name</th><th>Solved</th><th>Tests</th><th>Avg %</th><th>Points</th></tr>
            </thead>
            <tbody>
              {rows.map((r) => (
                <tr key={r.rank} className={r.isYou ? "you-row" : ""}>
                  <td>{r.rank <= 3 ? ["🥇", "🥈", "🥉"][r.rank - 1] : r.rank}</td>
                  <td>{r.name}{r.isYou && " (you)"}</td>
                  <td>{r.problemsSolved}</td>
                  <td>{r.mockTestsTaken}</td>
                  <td>{r.averagePercent}</td>
                  <td><strong>{r.points}</strong></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}
