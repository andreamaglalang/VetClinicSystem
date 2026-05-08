const fs = require("fs");
const path = require("path");

const inputPath = path.resolve(process.argv[2] || "VetClinicSystem_Functionality_Documentation.md");
const outputPath = path.resolve(process.argv[3] || "VetClinicSystem_Functionality_Documentation.pdf");

const markdown = fs.readFileSync(inputPath, "utf8").replace(/\r\n/g, "\n");

function normalizeLines(source) {
  const raw = source.split("\n");
  const result = [];
  let inTable = false;

  for (const line of raw) {
    const trimmed = line.trim();

    if (!trimmed) {
      result.push("");
      inTable = false;
      continue;
    }

    if (/^#{1,4}\s+/.test(trimmed)) {
      result.push(trimmed.replace(/^#{1,4}\s+/, "").toUpperCase());
      inTable = false;
      continue;
    }

    if (/^---+$/.test(trimmed)) {
      result.push("=".repeat(90));
      inTable = false;
      continue;
    }

    if (/^\|.*\|$/.test(trimmed)) {
      const cells = trimmed.slice(1, -1).split("|").map(x => x.trim());
      if (!cells.every(cell => /^:?-{3,}:?$/.test(cell))) {
        if (!inTable) {
          result.push("");
          inTable = true;
        }
        result.push(cells.join(" | "));
      }
      continue;
    }

    inTable = false;

    if (/^- \[ \]\s+/.test(trimmed)) {
      result.push("[ ] " + trimmed.replace(/^- \[ \]\s+/, ""));
      continue;
    }

    if (/^- /.test(trimmed)) {
      result.push("- " + trimmed.slice(2));
      continue;
    }

    result.push(trimmed);
  }

  return result;
}

function wrapLine(text, width) {
  if (!text) return [""];
  const words = text.split(/\s+/);
  const lines = [];
  let current = "";

  for (const word of words) {
    if (!current) {
      current = word;
      continue;
    }

    if ((current + " " + word).length <= width) {
      current += " " + word;
    } else {
      lines.push(current);
      current = word;
    }
  }

  if (current) lines.push(current);
  return lines;
}

function escapePdfText(text) {
  return text.replace(/\\/g, "\\\\").replace(/\(/g, "\\(").replace(/\)/g, "\\)");
}

const normalized = normalizeLines(markdown);
const wrapped = [];

for (const line of normalized) {
  if (line.length > 100) {
    wrapped.push(...wrapLine(line, 100));
  } else {
    wrapped.push(line);
  }
}

const pageWidth = 595;
const pageHeight = 842;
const marginLeft = 42;
const marginTop = 44;
const marginBottom = 42;
const lineHeight = 14;
const fontSize = 11;
const usableHeight = pageHeight - marginTop - marginBottom;
const linesPerPage = Math.floor(usableHeight / lineHeight);

const pages = [];
for (let i = 0; i < wrapped.length; i += linesPerPage) {
  pages.push(wrapped.slice(i, i + linesPerPage));
}

const objects = [];

function addObject(content) {
  objects.push(content);
  return objects.length;
}

const fontRegularId = addObject("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
const fontBoldId = addObject("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");

const pageIds = [];

for (const pageLines of pages) {
  let stream = "BT\n";
  stream += `/F1 ${fontSize} Tf\n`;
  stream += `${lineHeight} TL\n`;
  stream += `${marginLeft} ${pageHeight - marginTop} Td\n`;

  pageLines.forEach((line, index) => {
    const isHeading = /^[A-Z0-9 .,:;()\/&-]{4,}$/.test(line) && !line.startsWith("- ") && !line.startsWith("[ ]");
    if (index > 0) {
      stream += "T*\n";
    }
    if (isHeading && line.trim()) {
      stream += `/F2 12 Tf (${escapePdfText(line)}) Tj\n/F1 ${fontSize} Tf\n`;
    } else {
      stream += `(${escapePdfText(line)}) Tj\n`;
    }
  });

  stream += "ET";

  const contentStream = `<< /Length ${Buffer.byteLength(stream, "utf8")} >>\nstream\n${stream}\nendstream`;
  const contentId = addObject(contentStream);
  const pageId = addObject(`<< /Type /Page /Parent PAGES_REF 0 R /MediaBox [0 0 ${pageWidth} ${pageHeight}] /Resources << /Font << /F1 ${fontRegularId} 0 R /F2 ${fontBoldId} 0 R >> >> /Contents ${contentId} 0 R >>`);
  pageIds.push(pageId);
}

const pagesId = addObject(`<< /Type /Pages /Count ${pageIds.length} /Kids [${pageIds.map(id => `${id} 0 R`).join(" ")}] >>`);
const catalogId = addObject(`<< /Type /Catalog /Pages ${pagesId} 0 R >>`);

objects.forEach((object, index) => {
  if (index + 1 >= 3 && pageIds.includes(index + 1)) {
    objects[index] = object.replace("PAGES_REF", String(pagesId));
  }
});

let pdf = "%PDF-1.4\n";
const offsets = [0];

for (let i = 0; i < objects.length; i++) {
  offsets.push(Buffer.byteLength(pdf, "utf8"));
  pdf += `${i + 1} 0 obj\n${objects[i]}\nendobj\n`;
}

const xrefStart = Buffer.byteLength(pdf, "utf8");
pdf += `xref\n0 ${objects.length + 1}\n`;
pdf += "0000000000 65535 f \n";

for (let i = 1; i < offsets.length; i++) {
  pdf += `${String(offsets[i]).padStart(10, "0")} 00000 n \n`;
}

pdf += `trailer\n<< /Size ${objects.length + 1} /Root ${catalogId} 0 R >>\nstartxref\n${xrefStart}\n%%EOF`;

fs.writeFileSync(outputPath, pdf, "binary");
console.log(`Generated PDF: ${outputPath}`);
