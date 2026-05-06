document.addEventListener("DOMContentLoaded", function () {
    setupTablePanels();
    setupTablePagination();
    setupActionModals();
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

            const previous = makeButton("\u2039", Math.max(1, currentPage - 1), { label: "Previous page" });
            previous.classList.add("table-pagination__arrow");
            previous.disabled = currentPage === 1;
            buttons.appendChild(previous);

            const pageStatus = document.createElement("span");
            pageStatus.className = "table-pagination__page-status";
            pageStatus.textContent = currentPage + " OF " + pageCount;
            pageStatus.setAttribute("aria-current", "page");
            buttons.appendChild(pageStatus);

            const next = makeButton("\u203A", Math.min(pageCount, currentPage + 1), { label: "Next page" });
            next.classList.add("table-pagination__arrow");
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

function setupActionModals() {
    if (!window.bootstrap || !window.bootstrap.Modal) {
        return;
    }

    const modalElement = ensureActionModal();
    const modal = new window.bootstrap.Modal(modalElement);
    const titleElement = modalElement.querySelector("[data-action-modal-title]");
    const bodyElement = modalElement.querySelector("[data-action-modal-body]");

    document.addEventListener("click", function (event) {
        const link = event.target.closest("a[href]");

        if (!link || !shouldOpenActionModal(link)) {
            return;
        }

        event.preventDefault();
        openActionModal(link.href);
    });

    bodyElement.addEventListener("click", function (event) {
        const closeLink = event.target.closest("a.btn-secondary, a.btn-outline-secondary");

        if (closeLink) {
            event.preventDefault();
            modal.hide();
        }
    });

    bodyElement.addEventListener("submit", function (event) {
        const form = event.target;

        if (!(form instanceof HTMLFormElement)) {
            return;
        }

        event.preventDefault();
        submitModalForm(form, event.submitter);
    });

    async function openActionModal(url) {
        setModalLoading("Loading...");
        modal.show();

        try {
            const response = await fetch(url, {
                headers: { "X-Requested-With": "XMLHttpRequest" },
                credentials: "same-origin"
            });

            if (!response.ok) {
                throw new Error("Unable to load this action.");
            }

            const html = await response.text();
            renderModalHtml(html, response.url || url);
        } catch (error) {
            renderModalError(error.message);
        }
    }

    async function submitModalForm(form, submitter) {
        const actionUrl = form.action || modalElement.dataset.actionUrl;
        const method = (form.method || "post").toUpperCase();
        const formData = submitter ? new FormData(form, submitter) : new FormData(form);

        setModalBusy(true);

        try {
            const response = await fetch(actionUrl, {
                method: method,
                body: formData,
                headers: { "X-Requested-With": "XMLHttpRequest" },
                credentials: "same-origin"
            });

            if (response.redirected) {
                window.location.reload();
                return;
            }

            if (!response.ok) {
                throw new Error("Unable to complete this action.");
            }

            const html = await response.text();
            renderModalHtml(html, response.url || actionUrl);
        } catch (error) {
            renderModalError(error.message);
        } finally {
            setModalBusy(false);
        }
    }

    function renderModalHtml(html, actionUrl) {
        const doc = new DOMParser().parseFromString(html, "text/html");
        const content = doc.querySelector(".app-container--standard") || doc.querySelector("main") || doc.body;
        const heading = content.querySelector("h1, h2, h3");

        titleElement.textContent = heading ? heading.textContent.trim() : "Action";

        if (heading) {
            heading.remove();
        }

        bodyElement.innerHTML = content.innerHTML;
        modalElement.dataset.actionUrl = actionUrl;
        normalizeModalForms(actionUrl);
    }

    function normalizeModalForms(actionUrl) {
        bodyElement.querySelectorAll("form").forEach(function (form) {
            const actionAttribute = form.getAttribute("action");
            form.action = actionAttribute ? new URL(actionAttribute, actionUrl).href : actionUrl;
        });
    }

    function setModalLoading(title) {
        titleElement.textContent = title;
        bodyElement.innerHTML = '<div class="action-modal-loading">Please wait...</div>';
    }

    function setModalBusy(isBusy) {
        bodyElement.querySelectorAll("button, input, select, textarea").forEach(function (element) {
            element.disabled = isBusy;
        });
    }

    function renderModalError(message) {
        titleElement.textContent = "Action unavailable";
        bodyElement.innerHTML = '<div class="alert alert-danger">' + escapeHtml(message) + '</div>';
    }
}

function shouldOpenActionModal(link) {
    if (link.target && link.target !== "_self") {
        return false;
    }

    const url = new URL(link.href, window.location.href);

    if (url.origin !== window.location.origin) {
        return false;
    }

    const path = url.pathname.toLowerCase();
    const isModalAction = /\/(details|edit|delete)(\/|$)/.test(path);
    const isActionLink = link.closest(".table-actions") || link.classList.contains("table-action-icon") || link.closest(".app-container--standard");

    return isModalAction && Boolean(isActionLink);
}

function ensureActionModal() {
    let modal = document.getElementById("actionModal");

    if (modal) {
        return modal;
    }

    modal = document.createElement("div");
    modal.className = "modal fade action-modal";
    modal.id = "actionModal";
    modal.tabIndex = -1;
    modal.setAttribute("aria-hidden", "true");
    modal.innerHTML = [
        '<div class="modal-dialog modal-dialog-centered action-modal-dialog">',
        '  <div class="modal-content action-modal-content">',
        '    <div class="modal-header action-modal-header">',
        '      <h2 class="modal-title" data-action-modal-title>Action</h2>',
        '      <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>',
        '    </div>',
        '    <div class="modal-body action-modal-body" data-action-modal-body></div>',
        '  </div>',
        '</div>'
    ].join("");

    document.body.appendChild(modal);
    return modal;
}

function escapeHtml(value) {
    return String(value)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}
