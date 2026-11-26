import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { api } from "../api";
import { Difficulty, ErrorNote } from "../components.jsx";

export function CompanyGrid() {
  const [companies, setCompanies] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    api.companies().then(setCompanies).catch((e) => setError(e.message));
  }, []);

  if (error) return <ErrorNote message={`${error}. Is the API running on port 5080?`} />;
  if (!companies) return <p className="muted">Loading…</p>;

  return (
    <div className="grid">
      {companies.map((c) => (
        <Link to={`/companies/${c.slug}`} className="card company-card" key={c.id}>
          <div className="card-top">
            <span className="emoji">{c.emoji}</span>
            <Difficulty level={c.difficulty} />
          </div>
          <h3>{c.name}</h3>
          <p className="muted">{c.description}</p>
          <p className="meta">{c.paperCount} mock papers</p>
        </Link>
      ))}
    </div>
  );
}

export default function Companies() {
  return (
    <>
      <h1>Company-wise mock tests</h1>
      <p className="muted">
        Pick a company to see its typical hiring pattern and practise with papers modelled on it. You can also
        generate fresh AI papers for any company.
      </p>
      <CompanyGrid />
    </>
  );
}
