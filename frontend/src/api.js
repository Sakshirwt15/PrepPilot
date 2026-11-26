// Thin wrapper around fetch for the .NET API.
// In dev, Vite proxies /api to http://localhost:5080. For a deployed build, set VITE_API_URL.
const BASE = import.meta.env.VITE_API_URL ?? "";

let token = localStorage.getItem("token");
export const getToken = () => token;
export const setToken = (value) => {
  token = value;
  if (value) localStorage.setItem("token", value);
  else localStorage.removeItem("token");
};

async function request(path, options = {}) {
  const headers = { "Content-Type": "application/json", ...(token ? { Authorization: `Bearer ${token}` } : {}) };
  const response = await fetch(`${BASE}${path}`, { ...options, headers });

  if (response.status === 401 && token) {
    // Token expired or invalid: forget it and tell the app.
    setToken(null);
    window.dispatchEvent(new Event("auth:expired"));
  }

  if (!response.ok) {
    let message = `Request failed (${response.status})`;
    if (response.status === 401) message = "Please log in to continue.";
    if (response.status === 429) message = "Too many requests. Please slow down for a minute.";
    try {
      const problem = await response.json();
      if (problem.errors) message = Object.values(problem.errors).flat().join(" ");
      else if (problem.detail) message = problem.detail;
      else if (problem.title && response.status !== 401 && response.status !== 429) message = problem.title;
    } catch {
      /* response had no JSON body */
    }
    throw new Error(message);
  }

  return response.status === 204 ? null : response.json();
}

const post = (path, body) =>
  request(path, { method: "POST", body: body === undefined ? undefined : JSON.stringify(body) });

const query = (params) => {
  const search = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => value && search.set(key, value));
  const text = search.toString();
  return text ? `?${text}` : "";
};

export const api = {
  register: (dto) => post("/api/auth/register", dto),
  login: (dto) => post("/api/auth/login", dto),
  me: () => request("/api/auth/me"),

  companies: () => request("/api/companies"),
  company: (slug) => request(`/api/companies/${slug}`),
  mockTest: (id) => request(`/api/mock-tests/${id}`),

  startAttempt: (mockTestId) => post("/api/attempts", { mockTestId }),
  submitAttempt: (id, answers) => post(`/api/attempts/${id}/submit`, { answers }),
  attemptResult: (id) => request(`/api/attempts/${id}`),

  problems: (filters = {}) => request(`/api/problems${query(filters)}`),
  problem: (slug) => request(`/api/problems/${slug}`),
  submitSolution: (id, dto) => post(`/api/problems/${id}/submissions`, dto),
  submissions: (id) => request(`/api/problems/${id}/submissions`),
  coach: (id, dto) => post(`/api/problems/${id}/coach`, dto),

  generatePaper: (dto) => post("/api/ai/generate-paper", dto),
  interview: (dto) => post("/api/ai/interview", dto),

  dashboard: () => request("/api/me/dashboard"),
  leaderboard: () => request("/api/leaderboard"),
};
