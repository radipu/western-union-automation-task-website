"use strict";

// SignalR
const hub = new signalR.HubConnectionBuilder()
  .withUrl("/progressHub")
  .withAutomaticReconnect()
  .build();

hub.on("ProgressUpdate", p => {
  appendLog(p.username, p.message, p.status);
  if (p.total > 0) {
    const pct = Math.round((p.completed / p.total) * 100);
    document.getElementById("progressFill").style.width = pct + "%";
    document.getElementById("progressCount").textContent = `${p.completed} / ${p.total}`;
    document.getElementById("progressMsg").textContent = p.message;
    updatePreviewStatus(p.username, p.status);
  }
});

hub.on("SessionComplete", r => {
  document.getElementById("spinIcon").style.animation = "none";
  document.getElementById("spinIcon").textContent = r.failed === 0 ? "✔" : "⚠";
  document.getElementById("spinIcon").style.color = r.failed === 0 ? "var(--success)" : "var(--warn)";
  document.getElementById("progressMsg").textContent =
    `Done — ${r.success} succeeded, ${r.failed} failed out of ${r.total}.`;
  document.getElementById("progressFill").style.width = "100%";
  document.getElementById("btnRun").disabled = false;
  document.getElementById("btnDownload").disabled = false;
  loadResults();
});

hub.start().catch(e => console.error("SignalR error:", e));

// File upload
async function handleFileUpload(input) {
  const file = input.files[0];
  if (!file) return;

  document.getElementById("uploadFilename").textContent = "⏳ Parsing " + file.name + "…";
  document.getElementById("uploadFilename").classList.remove("hidden");

  const form = new FormData();
  form.append("file", file);

  try {
    const res = await fetch("/api/automation/upload", { method: "POST", body: form });
    const data = await res.json();

    if (!res.ok) { alert(data.message || "Upload failed."); return; }

    document.getElementById("uploadFilename").textContent = `✔ ${file.name} — ${data.count} customer(s) loaded`;
    document.getElementById("uploadZone").style.borderColor = "var(--success)";

    // Hide upload zone, show preview
    document.getElementById("emptyPreview").classList.add("hidden");
    document.getElementById("previewTableWrap").classList.remove("hidden");
    document.getElementById("previewTitle").textContent = `Customer Queue (${data.count})`;

    const tbody = document.getElementById("previewBody");
    tbody.innerHTML = "";

    data.customers.forEach((c, i) => {
      const tr = document.createElement("tr");
      tr.id = "preview-" + c.username;
      tr.innerHTML = `
        <td>${c.rowNumber}</td>
        <td><strong>${c.fullName || c.username}</strong></td>
        <td class="mono">@${c.username}</td>
        <td><span class="pill pill-pending">${c.accountType}</span></td>
        <td class="mono">$${(c.initialDeposit || 0).toFixed(2)}</td>
        <td class="mono">$${(c.downPayment || 0).toFixed(2)}</td>
        <td id="pstatus-${c.username}"><span class="pill pill-pending">Pending</span></td>
      `;
      tbody.appendChild(tr);
    });

    document.getElementById("btnRun").disabled = false;

  } catch (err) {
    alert("Error uploading file: " + err.message);
  }
}

// Drag-and-drop support
const zone = document.getElementById("uploadZone");
zone.addEventListener("dragover", e => { e.preventDefault(); zone.classList.add("drag-over"); });
zone.addEventListener("dragleave", () => zone.classList.remove("drag-over"));
zone.addEventListener("drop", e => {
  e.preventDefault();
  zone.classList.remove("drag-over");
  const dt = new DataTransfer();
  dt.items.add(e.dataTransfer.files[0]);
  const fi = document.getElementById("fileInput");
  fi.files = dt.files;
  handleFileUpload(fi);
});

// Run automation
async function startRun() {
  document.getElementById("btnRun").disabled = true;
  document.getElementById("btnDownload").disabled = true;
  document.getElementById("progressBanner").classList.remove("hidden");
  document.getElementById("resultsSection").classList.add("hidden");

  const spinEl = document.getElementById("spinIcon");
  spinEl.style.animation = "spin 1s linear infinite";
  spinEl.style.color = "var(--accent)";
  spinEl.textContent = "⟳";
  document.getElementById("progressFill").style.width = "0%";
  document.getElementById("progressMsg").textContent = "Starting automation…";
  document.getElementById("progressCount").textContent = "";

  // Clear log
  document.getElementById("logBox").innerHTML = "";

  try {
    const res = await fetch("/api/automation/run", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ headless: false })
    });
    const data = await res.json();
    if (!res.ok) {
      alert(data.message || "Failed to start.");
      document.getElementById("btnRun").disabled = false;
    }
  } catch (err) {
    alert("Error: " + err.message);
    document.getElementById("btnRun").disabled = false;
  }
}

// Download report
function downloadReport() {
  window.location.href = "/api/automation/report";
}

// Load results
async function loadResults() {
  const res = await fetch("/api/automation/status");
  const data = await res.json();
  if (!data.hasSession || !data.results?.length) return;

  document.getElementById("resultsSection").classList.remove("hidden");
  const tbody = document.getElementById("resultsBody");
  tbody.innerHTML = "";

  data.results.forEach((r, i) => {
    const ok = r.status === "Completed";
    const loanOk = r.loanStatus.toLowerCase().includes("approved");
    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>${i + 1}</td>
      <td>
        <strong>${r.name}</strong><br>
        <span class="mono muted-text">@${r.username}</span>
      </td>
      <td class="mono">${r.account || "—"}</td>
      <td class="mono">$${(r.loanUsd || 0).toFixed(2)}</td>
      <td class="mono">$${(r.downUsd || 0).toFixed(2)}</td>
      <td class="mono">€${(r.loanEur || 0).toFixed(2)}</td>
      <td class="mono">€${(r.downEur || 0).toFixed(2)}</td>
      <td><span class="pill ${loanOk ? 'pill-ok' : 'pill-warn'}">${r.loanStatus || "—"}</span></td>
      <td><span class="pill ${ok ? 'pill-ok' : 'pill-fail'}">${r.status}</span></td>
    `;
    tbody.appendChild(tr);

    // Update preview status
    updatePreviewStatus(r.username, ok ? "Success" : "Failed");
  });
}

// UI helpers

function updatePreviewStatus(username, status) {
  const el = document.getElementById("pstatus-" + username);
  if (!el) return;
  const map = {
    Running: ["pill-pending", "⟳ Running"],
    Success: ["pill-ok",      "✔ Done"],
    Failed:  ["pill-fail",    "✘ Failed"],
    Completed: ["pill-ok",   "✔ Done"]
  };
  const [cls, label] = map[status] || ["pill-pending", status];
  el.innerHTML = `<span class="pill ${cls}">${label}</span>`;
}

function appendLog(username, message, status) {
  const box = document.getElementById("logBox");
  // Remove "empty" placeholder
  box.querySelectorAll(".log-empty").forEach(e => e.remove());

  const now = new Date().toLocaleTimeString("en-GB", { hour12: false });
  const msgClass = status === "Success" || status === "Completed" ? "ok"
                 : status === "Failed" ? "fail" : "";

  const div = document.createElement("div");
  div.className = "log-line";
  div.innerHTML = `
    <span class="log-time">${now}</span>
    <span class="log-user">${username || "sys"}</span>
    <span class="log-msg ${msgClass}">${message}</span>
  `;
  box.appendChild(div);
  box.scrollTop = box.scrollHeight;
}


// Event wiring
document.getElementById("fileInput")?.addEventListener("change", e => handleFileUpload(e.target));
document.getElementById("fileInput2")?.addEventListener("change", e => handleFileUpload(e.target));
document.getElementById("btnRun")?.addEventListener("click", startRun);
document.getElementById("btnDownload")?.addEventListener("click", downloadReport);
document.getElementById("uploadZone")?.addEventListener("click", e => {
  if (e.target?.tagName?.toLowerCase() === "input") return;
  document.getElementById("fileInput").click();
});

// Restore state if a previous session exists
(async function restoreState() {
  try {
    const res = await fetch("/api/automation/status");
    const data = await res.json();
    if (!data.hasSession) return;
    if (data.hasReport) document.getElementById("btnDownload").disabled = false;
    if (!data.isRunning && data.results?.length) loadResults();
  } catch {  }
})();
