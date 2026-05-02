document.addEventListener("DOMContentLoaded", function () {
    setupTablePanels();
    setupTablePagination();
});

function setupTablePanels() {
    const tables = document.querySelectorAll(".app-container--standard > table.table");

    tables.forEach(function (table) {
        if (table.closest(".medivet-price-panel") || table.closest(".table-panel")) {
            return;
        }

        const items = [];
        const previous = table.previousElementSibling;

        if (previous && previous.tagName === "P") {
            items.unshift(previous);
            const beforePrevious = previous.previousElementSibling;

            if (beforePrevious && beforePrevious.matches('form[method="get"]')) {
                items.unshift(beforePrevious);
            }
        } else if (previous && previous.matches('form[method="get"]')) {
            items.unshift(previous);
        }

        const panel = document.createElement("section");
        panel.className = "table-panel";
        table.parentNode.insertBefore(panel, table);

        items.push(table);
        items.forEach(function (item) {
            panel.appendChild(item);
        });
    });
}

function setupTablePagination() {
    const pageSize = 8;
    const tables = document.querySelectorAll(".app-container--standard table.table");

    tables.forEach(function (table, index) {
        if (table.closest(".medivet-price-panel")) {
            return;
        }

        const tbody = table.querySelector("tbody");
        const rows = tbody ? Array.from(tbody.querySelectorAll(":scope > tr")) : [];
        const hasHeader = Boolean(table.querySelector("thead"));

        if (!hasHeader || rows.length <= pageSize || table.dataset.paginated === "true") {
            return;
        }

        table.dataset.paginated = "true";
        let currentPage = 1;
        const pageCount = Math.ceil(rows.length / pageSize);

        const pagination = document.createElement("nav");
        pagination.className = "table-pagination";
        pagination.setAttribute("aria-label", "Table pagination");

        const summary = document.createElement("span");
        summary.className = "table-pagination__summary";

        const buttons = document.createElement("div");
        buttons.className = "table-pagination__buttons";

        pagination.append(summary, buttons);
        table.insertAdjacentElement("afterend", pagination);

        function makeButton(label, page, options) {
            const button = document.createElement("button");
            button.type = "button";
            button.textContent = label;

            if (options && options.label) {
                button.setAttribute("aria-label", options.label);
            }

            button.addEventListener("click", function () {
                currentPage = page;
                render();
            });

            return button;
        }

        function render() {
            const start = (currentPage - 1) * pageSize;
            const end = start + pageSize;

            rows.forEach(function (row, rowIndex) {
                row.hidden = rowIndex < start || rowIndex >= end;
            });

            buttons.innerHTML = "";

            const previous = makeButton("Previous", Math.max(1, currentPage - 1), { label: "Previous page" });
            previous.disabled = currentPage === 1;
            buttons.appendChild(previous);

            for (let page = 1; page <= pageCount; page += 1) {
                const pageButton = makeButton(String(page), page, { label: "Page " + page });
                if (page === currentPage) {
                    pageButton.classList.add("is-active");
                    pageButton.setAttribute("aria-current", "page");
                }
                buttons.appendChild(pageButton);
            }

            const next = makeButton("Next", Math.min(pageCount, currentPage + 1), { label: "Next page" });
            next.disabled = currentPage === pageCount;
            buttons.appendChild(next);

            const visibleStart = start + 1;
            const visibleEnd = Math.min(end, rows.length);
            summary.textContent = "Showing " + visibleStart + "-" + visibleEnd + " of " + rows.length + " records";

            pagination.id = "table-pagination-" + index;
            table.setAttribute("aria-describedby", pagination.id);
        }

        render();
    });
}
