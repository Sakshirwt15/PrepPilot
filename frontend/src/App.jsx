import { Link, NavLink, Route, Routes, useNavigate } from "react-router-dom";
import { RequireAuth, useAuth } from "./auth.jsx";
import Home from "./pages/Home.jsx";
import Auth from "./pages/Auth.jsx";
import Companies from "./pages/Companies.jsx";
import CompanyDetail from "./pages/CompanyDetail.jsx";
import Generate from "./pages/Generate.jsx";
import Take from "./pages/Take.jsx";
import Result from "./pages/Result.jsx";
import Practice from "./pages/Practice.jsx";
import Problem from "./pages/Problem.jsx";
import Dashboard from "./pages/Dashboard.jsx";
import Leaderboard from "./pages/Leaderboard.jsx";
import Interview from "./pages/Interview.jsx";

export default function App() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  return (
    <>
      <header className="topbar">
        <div className="container topbar-inner">
          <Link to="/" className="brand">
            Prep<span>Pilot</span>
          </Link>
          <nav>
            <NavLink to="/companies">Companies</NavLink>
            <NavLink to="/practice">DSA Practice</NavLink>
            <NavLink to="/interview">AI Interview</NavLink>
            <NavLink to="/leaderboard">Leaderboard</NavLink>
          </nav>
          <div className="user-area">
            {user ? (
              <>
                <NavLink to="/dashboard" className="user-chip">👤 {user.fullName.split(" ")[0]}</NavLink>
                <button
                  className="btn small"
                  onClick={() => {
                    logout();
                    navigate("/");
                  }}
                >
                  Log out
                </button>
              </>
            ) : (
              <>
                <Link className="btn small" to="/login">Log in</Link>
                <Link className="btn small primary" to="/signup">Sign up</Link>
              </>
            )}
          </div>
        </div>
      </header>

      <main className="container">
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/login" element={<Auth mode="login" />} />
          <Route path="/signup" element={<Auth mode="signup" />} />
          <Route path="/companies" element={<Companies />} />
          <Route path="/companies/:slug" element={<CompanyDetail />} />
          <Route path="/practice" element={<Practice />} />
          <Route path="/practice/:slug" element={<Problem />} />
          <Route path="/leaderboard" element={<Leaderboard />} />
          <Route path="/generate" element={<RequireAuth><Generate /></RequireAuth>} />
          <Route path="/test/:id" element={<RequireAuth><Take /></RequireAuth>} />
          <Route path="/result/:attemptId" element={<RequireAuth><Result /></RequireAuth>} />
          <Route path="/dashboard" element={<RequireAuth><Dashboard /></RequireAuth>} />
          <Route path="/interview" element={<RequireAuth><Interview /></RequireAuth>} />
          <Route path="*" element={<p className="muted">Page not found.</p>} />
        </Routes>
      </main>

      <footer className="container footer">
        PrepPilot is a practice tool. Company papers are modelled on commonly reported patterns, not official or past papers.
      </footer>
    </>
  );
}
