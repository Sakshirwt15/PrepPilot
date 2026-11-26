import { Link } from "react-router-dom";
import { useAuth } from "../auth.jsx";
import { CompanyGrid } from "./Companies.jsx";

const FEATURES = [
  ["🏢", "Company-wise mock papers", "TCS, Infosys, Wipro, Accenture, Aon, Amazon and more, with timed tests and topic-wise analysis."],
  ["💻", "DSA practice with a real runner", "Write JavaScript and run it against test cases right in your browser. Python, Java and C++ get AI code review."],
  ["🤖", "AI mentor", "Progressive hints that do not spoil the answer, code reviews, and full explanations once you have solved a problem."],
  ["✨", "Unlimited fresh papers", "Generate a new company-style paper or topic quiz with AI whenever you run out of practice material."],
  ["🎤", "AI mock interviews", "Practise HR and technical interviews and get feedback after every answer."],
  ["📈", "Progress tracking", "Dashboard with weak topics, streaks and a leaderboard to keep you consistent."],
];

export default function Home() {
  const { user } = useAuth();

  return (
    <>
      <section className="hero">
        <h1>Crack your campus placements with <span>PrepPilot</span></h1>
        <p>
          Company-wise mock tests, DSA practice and an AI mentor in one place, so you know exactly what to
          improve before the real test.
        </p>
        <div className="actions center">
          {user ? (
            <Link className="btn primary big-btn" to="/dashboard">Go to my dashboard</Link>
          ) : (
            <>
              <Link className="btn primary big-btn" to="/signup">Start for free</Link>
              <Link className="btn big-btn" to="/practice">Try a DSA problem</Link>
            </>
          )}
        </div>
      </section>

      <section className="grid features">
        {FEATURES.map(([icon, title, text]) => (
          <div className="card feature" key={title}>
            <div className="emoji">{icon}</div>
            <h3>{title}</h3>
            <p className="muted">{text}</p>
          </div>
        ))}
      </section>

      <h2>Pick a company</h2>
      <CompanyGrid />
    </>
  );
}
