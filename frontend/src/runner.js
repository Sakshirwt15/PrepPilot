// Runs the student's JavaScript against the test cases inside a Web Worker.
// The worker has no access to the page, and is terminated if it runs too long (infinite loops).
const WORKER_SOURCE = `
self.onmessage = (event) => {
  const { code, fn, cases } = event.data;
  let userFn;
  try {
    userFn = new Function(code + "\\n;return typeof " + fn + " === 'function' ? " + fn + " : undefined;")();
  } catch (error) {
    self.postMessage({ error: String(error) });
    return;
  }
  if (!userFn) {
    self.postMessage({ error: "Function '" + fn + "' was not found. Keep the function name from the starter code." });
    return;
  }
  const results = [];
  for (const testCase of cases) {
    try {
      const args = JSON.parse(JSON.stringify(testCase.input));
      const got = userFn(...args);
      const gotText = JSON.stringify(got);
      results.push({ ok: gotText === JSON.stringify(testCase.expected), got: gotText === undefined ? "undefined" : gotText });
    } catch (error) {
      results.push({ ok: false, got: "", error: String(error) });
    }
  }
  self.postMessage({ results });
};
`;

export function runTests(code, functionName, testCases, timeoutMs = 4000) {
  return new Promise((resolve) => {
    const url = URL.createObjectURL(new Blob([WORKER_SOURCE], { type: "text/javascript" }));
    const worker = new Worker(url);

    const finish = (value) => {
      clearTimeout(timer);
      worker.terminate();
      URL.revokeObjectURL(url);
      resolve(value);
    };

    const timer = setTimeout(
      () => finish({ error: `Time limit exceeded (${timeoutMs / 1000}s). Check for an infinite loop or a slow algorithm.` }),
      timeoutMs
    );

    worker.onmessage = (event) => finish(event.data);
    worker.onerror = (event) => finish({ error: event.message || "Your code could not be run." });
    worker.postMessage({ code, fn: functionName, cases: testCases });
  });
}
