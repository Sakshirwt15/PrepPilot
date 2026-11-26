/** Renders AI text: paragraphs plus ``` code fences. Never injects HTML. */
export function RichText({ text }) {
  const parts = (text ?? "").split(/```[\w+-]*\n?/);
  return (
    <div className="rich">
      {parts.map((part, index) => {
        if (index % 2 === 1) {
          return (
            <pre key={index}>
              <code>{part.trim()}</code>
            </pre>
          );
        }
        return part.trim() ? <p key={index}>{part.trim()}</p> : null;
      })}
    </div>
  );
}

export function Difficulty({ level }) {
  return <span className={`badge ${level?.toLowerCase()}`}>{level}</span>;
}

export function Bar({ percent, tone }) {
  const value = Math.max(0, Math.min(100, percent ?? 0));
  const color = tone ?? (value >= 70 ? "good" : value >= 40 ? "mid" : "bad");
  return (
    <div className="bar">
      <div className={`bar-fill ${color}`} style={{ width: `${value}%` }} />
    </div>
  );
}

export function ErrorNote({ message }) {
  return message ? <p className="alert error">{message}</p> : null;
}
