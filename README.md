# PrepPilot: placement prep with AI

A full-stack platform to prepare for campus placements: **company-wise mock tests**, **DSA practice with an in-browser code runner**, an **AI mentor**, **AI-generated papers**, **AI mock interviews**, and a progress dashboard.

**Stack:** .NET 8 (ASP.NET Core Web API, EF Core, SQL Server, JWT auth) · React 18 + Vite · xUnit tests · Docker · AI via Groq (free tier) or Anthropic.

## Features

| Area | What you get |
|---|---|
| **Accounts** | Sign up / log in with JWT. Passwords are hashed with BCrypt. |
| **Company-wise mock tests** | 9 companies (TCS, Infosys, Wipro, Accenture, Cognizant, Capgemini, Deloitte, Aon, Amazon), each with its typical hiring pattern and 2 timed papers. |
| **Test experience** | Countdown timer with auto-submit, question palette, mark for review, and a leave-page warning. |
| **Results** | Score, pass/fail, **topic-wise analysis**, and a full review with correct answers and explanations. |
| **DSA practice** | 15 problems (easy to hard) filterable by difficulty, topic and company. Write JavaScript and **run it against test cases in your browser** (Web Worker with a time limit). Python, Java and C++ are supported with AI review. |
| **AI mentor** | Three progressive hint levels, code review, and a full explanation that **unlocks only after you solve the problem**. |
| **AI paper generator** | Fresh company-style paper or topic quiz on demand (5 per day per account). |
| **AI mock interview** | HR or technical interview: feedback after each answer and a final evaluation. |
| **Dashboard** | Streak, average and best score, solved problems by difficulty, topic accuracy, weak topics, recent tests. |
| **Leaderboard** | 10 points per solved problem, 5 per completed test. Names shown as "Sakshi R.". |

## Architecture

```
src/
  PrepPilot.Domain          Entities only
  PrepPilot.Application     Business logic: services, DTOs, scoring, streaks, AI prompts, repository interfaces
  PrepPilot.Infrastructure  EF Core + SQL Server, repositories, JWT/BCrypt, AI providers, embedded seed data
  PrepPilot.Api             Controllers, JWT setup, rate limiting, error middleware, Swagger
tests/PrepPilot.Tests       Unit, service and API integration tests (in-memory DB, fake AI)
frontend/                   React app (Vite)
tools/make_seed.py          Generates the seed JSON; expected DSA outputs come from reference solutions
database/useful-queries.sql Reporting queries (joins, aggregates)
```

Dependencies point inward: `Api -> Infrastructure -> Application -> Domain`.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js LTS](https://nodejs.org)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for SQL Server)

## Run it

**1. Database**
```bash
docker compose up -d db
```
Wait about 30 seconds for SQL Server to start.

**2. API** (terminal 1)
```bash
dotnet run --project src/PrepPilot.Api
```
Swagger: http://localhost:5080/swagger. In Development the API creates the database and seeds companies, papers and problems on startup.

**3. React app** (terminal 2)
```bash
cd frontend
npm install
npm run dev
```
Open **http://localhost:5173**, sign up, and start practising.

### Turn on the AI features (free)

Everything except AI works without a key. For AI hints, reviews, generated papers and interviews, get a free key from [Groq](https://console.groq.com) (**API Keys** -> **Create API Key**), then:

```bash
dotnet user-secrets set "Groq:ApiKey" "gsk_your_key_here" --project src/PrepPilot.Api
```
Restart the API. The default model is `llama-3.3-70b-versatile` (change `Groq:Model` if Groq retires it). The free tier has rate limits; if you see a "rate limit" message, wait a minute.

Prefer Claude? Set `Ai:Provider` to `Anthropic` and store `Anthropic:ApiKey` (paid API).

### Starting over / schema changes

The schema is created with `EnsureCreated`, which does not update existing tables. After pulling a version with schema changes, reset the database:
```bash
docker compose down -v
docker compose up -d db
```

## Tests

```bash
dotnet test
```
No SQL Server or API key is needed; tests use an in-memory database and a fake AI client. They cover scoring, streaks, auth, attempts and review, ownership checks, problem submissions, AI paper sanitising and limits, interview prompts, the dashboard and leaderboard, the embedded seed data, and the HTTP API end to end.

## API overview

| Method | Route | Auth | Purpose |
|---|---|---|---|
| POST | `/api/auth/register`, `/api/auth/login` | no | Create account / log in, returns a JWT |
| GET | `/api/auth/me` | yes | Current user |
| GET | `/api/companies`, `/api/companies/{slug}` | no | Companies, hiring pattern, papers |
| GET | `/api/mock-tests?company=&difficulty=` | no | List mock tests |
| GET | `/api/mock-tests/{id}` | no | Questions (never includes answers) |
| POST | `/api/attempts` | yes | Start a timed attempt |
| POST | `/api/attempts/{id}/submit` | yes | Submit answers, get graded result |
| GET | `/api/attempts/{id}`, `/api/attempts/mine` | yes | Result with review / history |
| GET | `/api/problems?difficulty=&topic=&company=&search=` | optional | List DSA problems (solved flags when signed in) |
| GET | `/api/problems/{slug}` | optional | Statement, starter code, test cases |
| POST | `/api/problems/{id}/submissions` | yes | Record a solution attempt |
| POST | `/api/problems/{id}/coach` | yes | AI hint / review / explain |
| POST | `/api/ai/generate-paper` | yes | Generate a company or topic paper |
| POST | `/api/ai/interview` | yes | One turn of an AI mock interview |
| GET | `/api/me/dashboard` | yes | Your progress |
| GET | `/api/leaderboard` | no | Top 20 |

## Design decisions

- **Correct answers never reach the browser before submission.** Test DTOs have no `IsCorrect` or explanation; an integration test checks the raw JSON.
- **Ownership is enforced server-side.** Someone else's attempt looks exactly like a missing one (404).
- **AI output is untrusted input.** It is parsed tolerantly, then sanitised: lengths clamped, invalid questions dropped (blank text, fewer than two options, not exactly one correct option), and a minimum number of valid questions is required. Nothing is saved otherwise.
- **AI is behind an interface** (`IAiClient`), so Groq/Anthropic are swappable and tests never touch the network.
- **AI abuse limits:** 15 AI requests per minute per user (ASP.NET rate limiter) and 5 generated papers per day per user.
- **Prompt-injection hygiene:** problem statements, user code and interview answers are passed as data with an instruction not to follow instructions found inside them.
- **Login errors are generic** ("Invalid email or password") so accounts cannot be enumerated.
- **Scoring and streaks are pure functions** with their own unit tests.
- **Seed data is validated by tests**, so a typo in the JSON fails a test instead of production startup.

## Honest limitations

- **Company papers are practice papers, not real ones.** They are written to resemble commonly reported patterns and are shared across companies from a small question pool. Hiring patterns change every season, so always check the official careers page. Use AI generation for fresh questions.
- **"Commonly reported at" company tags on DSA problems are approximate**, not an official list.
- **JavaScript results are computed in the browser**, so the server cannot fully verify them; a determined user could fake a pass. That is acceptable for personal practice, but not for a real contest. Python/Java/C++ "solved" marks are self-reported. A production system would run code in a sandbox (for example Judge0) on the server.
- **Mock-test timing is enforced in the browser** (the server stores the start time but does not reject late submissions).
- **AI can be wrong.** Explanations and reviews are guidance, not ground truth.
- The schema uses `EnsureCreated`; switch to EF Core migrations for production.
- The default JWT key in `appsettings.json` is for development only. Set `Jwt__Key` (32+ characters) via an environment variable when deploying.

## Ideas for next steps

Email verification and password reset, EF migrations, server-side code execution sandbox, more problems and papers (the seed JSON is easy to extend with `tools/make_seed.py`), resume and ATS feedback, deployment (Azure App Service + Static Web Apps).
