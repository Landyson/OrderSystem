async function api(url, options = {}) {
  const res = await fetch(url, {
    headers: { "Content-Type": "application/json" },
    ...options
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || ("HTTP " + res.status));
  }
  const ct = res.headers.get("content-type") || "";
  return ct.includes("application/json") ? res.json() : res.text();
}

function showMsg(el, text, ok=true) {
  el.className = "msg " + (ok ? "ok" : "err");
  el.textContent = text;
}
