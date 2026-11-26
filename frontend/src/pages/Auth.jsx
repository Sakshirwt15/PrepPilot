import { useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../auth.jsx";
import { ErrorNote } from "../components.jsx";

export default function Auth({ mode }) {
  const signup = mode === "signup";
  const { login, register } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [form, setForm] = useState({ fullName: "", email: "", password: "" });
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  const set = (field) => (e) => setForm({ ...form, [field]: e.target.value });

  const submit = async (e) => {
    e.preventDefault();
    setError("");
    setBusy(true);
    try {
      if (signup) await register(form.fullName, form.email, form.password);
      else await login(form.email, form.password);
      navigate(location.state?.from ?? "/dashboard", { replace: true });
    } catch (err) {
      setError(err.message);
      setBusy(false);
    }
  };

  return (
    <form className="card form narrow center-card" onSubmit={submit}>
      <h1>{signup ? "Create your account" : "Welcome back"}</h1>

      {signup && (
        <label>
          Full name
          <input value={form.fullName} onChange={set("fullName")} required minLength={2} maxLength={100} autoComplete="name" />
        </label>
      )}
      <label>
        Email
        <input type="email" value={form.email} onChange={set("email")} required autoComplete="email" />
      </label>
      <label>
        Password
        <input
          type="password"
          value={form.password}
          onChange={set("password")}
          required
          minLength={signup ? 8 : 1}
          autoComplete={signup ? "new-password" : "current-password"}
        />
        {signup && <span className="hint">At least 8 characters.</span>}
      </label>

      <ErrorNote message={error} />
      <button className="btn primary" disabled={busy}>
        {busy ? "Please wait…" : signup ? "Sign up" : "Log in"}
      </button>

      <p className="muted">
        {signup ? (
          <>Already have an account? <Link to="/login" state={location.state}>Log in</Link></>
        ) : (
          <>New here? <Link to="/signup" state={location.state}>Create an account</Link></>
        )}
      </p>
    </form>
  );
}
