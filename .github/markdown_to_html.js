const fs = require("fs");
const path = require("path");

const inputPath = path.resolve(process.argv[2] || "VetClinicSystem_Functionality_Documentation.md");
const outputPath = path.resolve(process.argv[3] || "VetClinicSystem_Functionality_Documentation.html");

const markdown = fs.readFileSync(inputPath, "utf8").replace(/\r\n/g, "\n");

function escapeHtml(value) {
  return String(value)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;");
}

function formatInline(text) {
  return escapeHtml(text)
    .replace(/`([^`]+)`/g, "<code>$1</code>")
    .replace(/\*\*([^*]+)\*\*/g, "<strong>$1</strong>");
}

function isTableLine(line) {
  return /^\|.*\|$/.test(line.trim());
}

function splitTableRow(line) {
  return line
    .trim()
    .slice(1, -1)
    .split("|")
    .map(cell => formatInline(cell.trim()));
}

function isTableSeparator(line) {
  return /^\|(?:\s*:?-{3,}:?\s*\|)+$/.test(line.trim());
}

const lines = markdown.split("\n");
const html = [];
let i = 0;

html.push(`<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>VetClinicSystem Functionality Documentation</title>
  <style>
    @page {
      size: A4;
      margin: 18mm 14mm;
    }
    body {
      font-family: Arial, Helvetica, sans-serif;
      color: #233942;
      line-height: 1.55;
      font-size: 12px;
      margin: 0;
      background: #ffffff;
    }
    h1, h2, h3, h4 {
      color: #0f8fa8;
      margin: 0 0 10px;
      line-height: 1.2;
    }
    h1 { font-size: 26px; margin-bottom: 16px; }
    h2 {
      font-size: 18px;
      margin-top: 28px;
      padding-bottom: 6px;
      border-bottom: 2px solid #b8ebf0;
    }
    h3 { font-size: 15px; margin-top: 18px; }
    h4 { font-size: 13px; margin-top: 14px; }
    p { margin: 0 0 10px; }
    hr {
      border: 0;
      border-top: 1px solid #d8ecf0;
      margin: 22px 0;
    }
    ul {
      margin: 0 0 12px 18px;
      padding: 0;
    }
    li { margin: 0 0 6px; }
    table {
      width: 100%;
      border-collapse: collapse;
      margin: 10px 0 18px;
      table-layout: fixed;
      page-break-inside: avoid;
    }
    th, td {
      border: 1px solid #d9e8ee;
      padding: 8px 9px;
      vertical-align: top;
      word-wrap: break-word;
    }
    th {
      background: #aeecef;
      color: #067f99;
      text-align: left;
      font-weight: 700;
    }
    tr:nth-child(even) td {
      background: #fbfefe;
    }
    code {
      background: #eef7f9;
      border-radius: 4px;
      padding: 1px 4px;
      font-family: Consolas, monospace;
      font-size: 11px;
    }
    .checklist-item {
      list-style: none;
      margin-left: -18px;
      padding-left: 0;
    }
    .checkbox {
      display: inline-block;
      width: 14px;
      color: #0f8fa8;
      font-weight: 700;
    }
  </style>
</head>
<body>`);

while (i < lines.length) {
  const rawLine = lines[i];
  const line = rawLine.trimEnd();
  const trimmed = line.trim();

  if (!trimmed) {
    i += 1;
    continue;
  }

  if (/^---+$/.test(trimmed)) {
    html.push("<hr />");
    i += 1;
    continue;
  }

  const headingMatch = /^(#{1,4})\s+(.*)$/.exec(trimmed);
  if (headingMatch) {
    const level = headingMatch[1].length;
    html.push(`<h${level}>${formatInline(headingMatch[2].trim())}</h${level}>`);
    i += 1;
    continue;
  }

  if (isTableLine(trimmed)) {
    const tableLines = [];
    while (i < lines.length && isTableLine(lines[i].trim())) {
      tableLines.push(lines[i].trim());
      i += 1;
    }

    const header = splitTableRow(tableLines[0]);
    const bodyLines = tableLines.slice(1).filter(row => !isTableSeparator(row));

    html.push("<table><thead><tr>");
    header.forEach(cell => html.push(`<th>${cell}</th>`));
    html.push("</tr></thead><tbody>");
    bodyLines.forEach(row => {
      const cells = splitTableRow(row);
      html.push("<tr>");
      cells.forEach(cell => html.push(`<td>${cell}</td>`));
      html.push("</tr>");
    });
    html.push("</tbody></table>");
    continue;
  }

  if (/^- /.test(trimmed)) {
    html.push("<ul>");
    while (i < lines.length && /^- /.test(lines[i].trim())) {
      const item = lines[i].trim().slice(2).trim();
      const checklistMatch = /^\[ \]\s+(.*)$/.exec(item);
      if (checklistMatch) {
        html.push(`<li class="checklist-item"><span class="checkbox">[ ]</span> ${formatInline(checklistMatch[1])}</li>`);
      } else {
        html.push(`<li>${formatInline(item)}</li>`);
      }
      i += 1;
    }
    html.push("</ul>");
    continue;
  }

  const paragraphLines = [trimmed];
  i += 1;
  while (i < lines.length) {
    const next = lines[i].trim();
    if (!next || /^#{1,4}\s+/.test(next) || /^---+$/.test(next) || isTableLine(next) || /^- /.test(next)) {
      break;
    }
    paragraphLines.push(next);
    i += 1;
  }

  html.push(`<p>${formatInline(paragraphLines.join(" "))}</p>`);
}

html.push("</body></html>");
fs.writeFileSync(outputPath, html.join("\n"), "utf8");
console.log(`Generated HTML: ${outputPath}`);
