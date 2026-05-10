document.addEventListener("DOMContentLoaded", function () {
    setupMobileNavigation();
    setupTablePanels(document);
    document.querySelectorAll(".table-panel, .medivet-price-panel").forEach(function (panel) {
        wrapTableForScroll(panel);
    });
    setupTablePagination(document);
    setupConfirmationForms();
    setupActionModals();
    setupInputSanitizers(document);
    setupAppointmentTimeSelectors(document);
});

function setupMobileNavigation() {
    const toggle = document.querySelector(".mobile-nav-toggle");
    const nav = document.getElementById("mainNavigation");

    if (!toggle || !nav) {
        return;
    }

    toggle.addEventListener("click", function () {
        const isOpen = nav.classList.toggle("is-open");
        toggle.setAttribute("aria-expanded", isOpen ? "true" : "false");
    });

    nav.querySelectorAll("a").forEach(function (link) {
        link.addEventListener("click", function () {
            nav.classList.remove("is-open");
            toggle.setAttribute("aria-expanded", "false");
        });
    });

    window.addEventListener("resize", function () {
        if (window.innerWidth > 760) {
            nav.classList.remove("is-open");
            toggle.setAttribute("aria-expanded", "false");
        }
    });
}

function setupTablePanels(root) {
    const container = root || document;
    const tables = container.querySelectorAll(".app-container--standard > table.table");

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

        wrapTableForScroll(panel);
    });
}

function setupTablePagination(root) {
    const pageSize = 8;
    const container = root || document;
    const tables = container.querySelectorAll(".app-container--standard table.table, .action-modal-body table.table");

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
        setupTablePanels(bodyElement);
        bodyElement.querySelectorAll(".table-panel, .medivet-price-panel").forEach(function (panel) {
            wrapTableForScroll(panel);
        });
        setupTablePagination(bodyElement);
        setupInputSanitizers(bodyElement);
        setupAppointmentTimeSelectors(bodyElement);
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

function setupConfirmationForms() {
    document.addEventListener("submit", function (event) {
        const form = event.target;

        if (!(form instanceof HTMLFormElement) || !form.dataset.confirmMessage || form.dataset.confirmed === "true") {
            return;
        }

        event.preventDefault();
        event.stopPropagation();

        showActionConfirmation(form.dataset.confirmMessage, function () {
            form.dataset.confirmed = "true";
            form.requestSubmit(event.submitter || undefined);
        });
    }, true);
}

function showActionConfirmation(message, onConfirm) {
    const overlay = ensureActionConfirmation();
    const messageElement = overlay.querySelector("[data-action-confirm-message]");
    const confirmButton = overlay.querySelector("[data-action-confirm-submit]");
    const cancelButtons = overlay.querySelectorAll("[data-action-confirm-cancel]");

    messageElement.textContent = message;
    overlay.hidden = false;
    confirmButton.focus();

    const close = function () {
        overlay.hidden = true;
        confirmButton.removeEventListener("click", confirm);
        cancelButtons.forEach(function (button) {
            button.removeEventListener("click", close);
        });
        overlay.removeEventListener("click", outsideClick);
    };

    const confirm = function () {
        close();
        onConfirm();
    };

    const outsideClick = function (event) {
        if (event.target === overlay) {
            close();
        }
    };

    confirmButton.addEventListener("click", confirm);
    cancelButtons.forEach(function (button) {
        button.addEventListener("click", close);
    });
    overlay.addEventListener("click", outsideClick);
}

function ensureActionConfirmation() {
    let overlay = document.getElementById("actionConfirmOverlay");

    if (overlay) {
        return overlay;
    }

    overlay = document.createElement("div");
    overlay.className = "admin-confirm-overlay action-confirm-overlay";
    overlay.id = "actionConfirmOverlay";
    overlay.hidden = true;
    overlay.innerHTML = [
        '<div class="admin-confirm-modal" role="dialog" aria-modal="true" aria-labelledby="actionConfirmTitle">',
        '  <button type="button" class="admin-confirm-close" data-action-confirm-cancel aria-label="Close confirmation"><i class="fa-solid fa-xmark"></i></button>',
        '  <span class="admin-confirm-icon"><i class="fa-solid fa-exclamation"></i></span>',
        '  <h3 id="actionConfirmTitle">Confirm Action</h3>',
        '  <p data-action-confirm-message></p>',
        '  <div class="admin-confirm-actions">',
        '    <button type="button" class="btn btn-danger" data-action-confirm-submit>Confirm</button>',
        '    <button type="button" class="btn btn-outline-secondary" data-action-confirm-cancel>Cancel</button>',
        '  </div>',
        '</div>'
    ].join("");

    document.body.appendChild(overlay);
    return overlay;
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

function wrapTableForScroll(root) {
    const container = root || document;
    const tables = container.querySelectorAll(":scope > table.table, :scope > .table");

    tables.forEach(function (table) {
        if (table.parentElement && table.parentElement.classList.contains("table-scroll")) {
            return;
        }

        const wrapper = document.createElement("div");
        wrapper.className = "table-scroll";
        table.parentNode.insertBefore(wrapper, table);
        wrapper.appendChild(table);
    });
}

function escapeHtml(value) {
    return String(value)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}

function setupInputSanitizers(root) {
    const container = root || document;

    function shouldTrimField(field) {
        if (!(field instanceof HTMLInputElement || field instanceof HTMLTextAreaElement)) {
            return false;
        }

        if (field instanceof HTMLTextAreaElement) {
            return true;
        }

        const type = (field.type || "text").toLowerCase();
        return type === "text" || type === "email" || type === "search" || type === "tel" || type === "url";
    }

    function trimFieldValue(field) {
        if (!shouldTrimField(field)) {
            return;
        }

        const trimmedValue = field.value.trim();
        if (field.value !== trimmedValue) {
            field.value = trimmedValue;
        }
    }

    container.querySelectorAll("input, textarea").forEach(function (field) {
        if (field.dataset.trimBound === "true") {
            return;
        }

        if (!shouldTrimField(field)) {
            return;
        }

        field.dataset.trimBound = "true";
        field.addEventListener("blur", function () {
            trimFieldValue(field);
        });
        field.addEventListener("change", function () {
            trimFieldValue(field);
        });
    });

    container.querySelectorAll("form").forEach(function (form) {
        if (form.dataset.trimSubmitBound === "true") {
            return;
        }

        form.dataset.trimSubmitBound = "true";
        form.addEventListener("submit", function () {
            form.querySelectorAll("input, textarea").forEach(function (field) {
                trimFieldValue(field);
            });
        }, true);
    });

    container.querySelectorAll("[data-letters-only='true']").forEach(function (input) {
        if (input.dataset.sanitizerBound === "true") {
            return;
        }

        input.dataset.sanitizerBound = "true";
        input.addEventListener("input", function () {
            input.value = input.value.replace(/[^A-Za-z ]+/g, "").replace(/\s{2,}/g, " ");
        });
    });

    container.querySelectorAll("[data-person-name='true']").forEach(function (input) {
        if (input.dataset.sanitizerBound === "true") {
            return;
        }

        input.dataset.sanitizerBound = "true";
        input.addEventListener("input", function () {
            input.value = input.value.replace(/[^A-Za-z' -]+/g, "").replace(/\s{2,}/g, " ");
        });
    });

    container.querySelectorAll("[data-username-only='true']").forEach(function (input) {
        if (input.dataset.sanitizerBound === "true") {
            return;
        }

        input.dataset.sanitizerBound = "true";
        input.addEventListener("input", function () {
            input.value = input.value.replace(/[^A-Za-z0-9_]+/g, "");
        });
    });

    container.querySelectorAll("[data-phone-only='true']").forEach(function (input) {
        if (input.dataset.sanitizerBound === "true") {
            return;
        }

        input.dataset.sanitizerBound = "true";
        input.addEventListener("input", function () {
            input.value = input.value.replace(/\D+/g, "").slice(0, 11);
        });
    });

    container.querySelectorAll("[data-integer-only='true']").forEach(function (input) {
        if (input.dataset.sanitizerBound === "true") {
            return;
        }

        input.dataset.sanitizerBound = "true";
        input.addEventListener("keydown", function (event) {
            const allowedKeys = [
                "Backspace",
                "Delete",
                "Tab",
                "Escape",
                "Enter",
                "ArrowLeft",
                "ArrowRight",
                "ArrowUp",
                "ArrowDown",
                "Home",
                "End"
            ];

            if (event.ctrlKey || event.metaKey) {
                return;
            }

            if (allowedKeys.includes(event.key)) {
                return;
            }

            if (!/^\d$/.test(event.key)) {
                event.preventDefault();
            }
        });

        input.addEventListener("paste", function (event) {
            const pastedText = (event.clipboardData || window.clipboardData)?.getData("text") ?? "";

            if (!/^\d+$/.test(pastedText.trim())) {
                event.preventDefault();
            }
        });

        input.addEventListener("input", function () {
            const start = input.selectionStart ?? input.value.length;
            const end = input.selectionEnd ?? input.value.length;
            const originalValue = input.value;
            const sanitizedValue = originalValue.replace(/\D+/g, "");

            if (sanitizedValue === originalValue) {
                return;
            }

            const sanitizedBeforeStart = originalValue.slice(0, start).replace(/\D+/g, "");
            const sanitizedBeforeEnd = originalValue.slice(0, end).replace(/\D+/g, "");

            input.value = sanitizedValue;

            if (typeof input.setSelectionRange === "function") {
                input.setSelectionRange(sanitizedBeforeStart.length, sanitizedBeforeEnd.length);
            }
        });
    });

    container.querySelectorAll("[data-address-safe='true']").forEach(function (input) {
        if (input.dataset.sanitizerBound === "true") {
            return;
        }

        input.dataset.sanitizerBound = "true";
        input.addEventListener("input", function () {
            input.value = input.value.replace(/[^A-Za-z0-9#.,/\- ]+/g, "").replace(/\s{2,}/g, " ");
        });
    });

    container.querySelectorAll("[data-decimal-only='true']").forEach(function (input) {
        if (input.dataset.sanitizerBound === "true") {
            return;
        }

        input.dataset.sanitizerBound = "true";
        input.addEventListener("keydown", function (event) {
            const allowedKeys = [
                "Backspace",
                "Delete",
                "Tab",
                "Escape",
                "Enter",
                "ArrowLeft",
                "ArrowRight",
                "ArrowUp",
                "ArrowDown",
                "Home",
                "End"
            ];

            if (event.ctrlKey || event.metaKey) {
                return;
            }

            if (allowedKeys.includes(event.key)) {
                return;
            }

            if (event.key === ".") {
                if (input.value.includes(".")) {
                    event.preventDefault();
                }
                return;
            }

            if (!/^\d$/.test(event.key)) {
                event.preventDefault();
            }
        });

        input.addEventListener("paste", function (event) {
            const pastedText = (event.clipboardData || window.clipboardData)?.getData("text") ?? "";

            if (!/^\d*\.?\d+$/.test(pastedText.trim())) {
                event.preventDefault();
            }
        });

        input.addEventListener("input", function () {
            const start = input.selectionStart ?? input.value.length;
            const end = input.selectionEnd ?? input.value.length;
            const originalValue = input.value;
            let dotFound = false;
            let sanitizedValue = "";

            for (const character of originalValue) {
                if (/\d/.test(character)) {
                    sanitizedValue += character;
                    continue;
                }

                if (character === "." && !dotFound) {
                    dotFound = true;
                    sanitizedValue += character;
                }
            }

            if (sanitizedValue === originalValue) {
                return;
            }

            const sanitizeSlice = function (value) {
                let sliceDotFound = false;
                let sliceResult = "";

                for (const character of value) {
                    if (/\d/.test(character)) {
                        sliceResult += character;
                        continue;
                    }

                    if (character === "." && !sliceDotFound) {
                        sliceDotFound = true;
                        sliceResult += character;
                    }
                }

                return sliceResult;
            };

            const sanitizedBeforeStart = sanitizeSlice(originalValue.slice(0, start));
            const sanitizedBeforeEnd = sanitizeSlice(originalValue.slice(0, end));

            input.value = sanitizedValue;

            if (typeof input.setSelectionRange === "function") {
                input.setSelectionRange(sanitizedBeforeStart.length, sanitizedBeforeEnd.length);
            }
        });
    });

    container.querySelectorAll("[data-color-only='true']").forEach(function (input) {
        if (input.dataset.sanitizerBound === "true") {
            return;
        }

        input.dataset.sanitizerBound = "true";
        input.addEventListener("input", function () {
            const start = input.selectionStart ?? input.value.length;
            const end = input.selectionEnd ?? input.value.length;
            const originalValue = input.value;
            const sanitizedValue = originalValue.replace(/[^A-Za-z, ]+/g, "");

            if (sanitizedValue === originalValue) {
                return;
            }

            const sanitizedBeforeStart = originalValue.slice(0, start).replace(/[^A-Za-z, ]+/g, "");
            const sanitizedBeforeEnd = originalValue.slice(0, end).replace(/[^A-Za-z, ]+/g, "");

            input.value = sanitizedValue;

            if (typeof input.setSelectionRange === "function") {
                input.setSelectionRange(sanitizedBeforeStart.length, sanitizedBeforeEnd.length);
            }
        });
    });
}

window.setupInputSanitizers = setupInputSanitizers;

function setupAppointmentTimeSelectors(root) {
    const container = root || document;
    const dateInputs = container.querySelectorAll("[data-appointment-date]");

    dateInputs.forEach(function (dateInput) {
        if (dateInput.dataset.timeSelectorBound === "true") {
            return;
        }

        const form = dateInput.closest("form");
        const timeSelect = form ? form.querySelector("[data-appointment-time]") : null;
        const dateMessage = form ? form.querySelector("[data-unavailable-date-message]") : null;
        const timeMessage = form ? form.querySelector("[data-clinic-time-message]") : null;

        if (!(timeSelect instanceof HTMLSelectElement) || !dateMessage || !timeMessage || !(form instanceof HTMLFormElement)) {
            return;
        }

        dateInput.dataset.timeSelectorBound = "true";
        const rangeMessage = dateInput.dataset.rangeMessage || "";
        const minDateValue = dateInput.dataset.minDate || "";
        const maxDateValue = dateInput.dataset.maxDate || "";

        dateInput.addEventListener("keydown", function (event) {
            if (event.ctrlKey || event.metaKey || event.altKey) {
                return;
            }

            const allowedKeys = [
                "Tab",
                "Shift",
                "ArrowLeft",
                "ArrowRight",
                "ArrowUp",
                "ArrowDown",
                "Home",
                "End",
                "Escape"
            ];

            if (allowedKeys.includes(event.key)) {
                return;
            }

            if (typeof dateInput.showPicker === "function") {
                dateInput.showPicker();
            }

            event.preventDefault();
        });

        function getEasterSunday(year) {
            const a = year % 19;
            const b = Math.floor(year / 100);
            const c = year % 100;
            const d = Math.floor(b / 4);
            const e = b % 4;
            const f = Math.floor((b + 8) / 25);
            const g = Math.floor((b - f + 1) / 3);
            const h = (19 * a + b - d - g + 15) % 30;
            const i = Math.floor(c / 4);
            const k = c % 4;
            const l = (32 + 2 * e + 2 * i - h - k) % 7;
            const m = Math.floor((a + 11 * h + 22 * l) / 451);
            const month = Math.floor((h + l - 7 * m + 114) / 31);
            const day = ((h + l - 7 * m + 114) % 31) + 1;

            return new Date(year, month - 1, day);
        }

        function toKey(date) {
            const year = date.getFullYear();
            const month = String(date.getMonth() + 1).padStart(2, "0");
            const day = String(date.getDate()).padStart(2, "0");
            return year + "-" + month + "-" + day;
        }

        function addDays(date, days) {
            const next = new Date(date);
            next.setDate(next.getDate() + days);
            return next;
        }

        function getUnavailableDateMessage(value) {
            if (!value) {
                return "";
            }

            const selectedDate = new Date(value + "T00:00:00");
            const minDate = minDateValue ? new Date(minDateValue + "T00:00:00") : null;
            const maxDate = maxDateValue ? new Date(maxDateValue + "T00:00:00") : null;
            const month = selectedDate.getMonth() + 1;
            const day = selectedDate.getDate();

            if ((minDate && selectedDate < minDate) || (maxDate && selectedDate > maxDate)) {
                return rangeMessage;
            }

            if (selectedDate.getDay() === 2) {
                return "The clinic is closed every Tuesday. Please choose another date.";
            }

            const fixedHolidays = {
                "1-1": "New Year's Day",
                "11-1": "All Saints' Day",
                "12-24": "Christmas Eve",
                "12-25": "Christmas Day",
                "12-31": "New Year's Eve"
            };

            const fixedHoliday = fixedHolidays[month + "-" + day];
            if (fixedHoliday) {
                return "The clinic is unavailable on " + fixedHoliday + ". Please choose another date.";
            }

            const easterSunday = getEasterSunday(selectedDate.getFullYear());
            const holyWeekDates = {};
            holyWeekDates[toKey(addDays(easterSunday, -3))] = "Maundy Thursday";
            holyWeekDates[toKey(addDays(easterSunday, -2))] = "Good Friday";
            holyWeekDates[toKey(addDays(easterSunday, -1))] = "Black Saturday";

            const holyWeekName = holyWeekDates[value];
            if (holyWeekName) {
                return "The clinic is unavailable on " + holyWeekName + ". Please choose another date.";
            }

            return "";
        }

        function getClinicHours(value) {
            if (!value) {
                return null;
            }

            const selectedDate = new Date(value + "T00:00:00");
            const day = selectedDate.getDay();

            if (day === 1) {
                return {
                    opening: "09:00",
                    closing: "17:00",
                    displayHours: "9:00 AM to 5:00 PM"
                };
            }

            if ([3, 4, 5, 6, 0].includes(day)) {
                return {
                    opening: "09:00",
                    closing: "19:00",
                    displayHours: "9:00 AM to 7:00 PM"
                };
            }

            return null;
        }

        function formatTimeLabel(totalMinutes) {
            const hours = Math.floor(totalMinutes / 60);
            const minutes = totalMinutes % 60;
            const period = hours >= 12 ? "PM" : "AM";
            const twelveHour = hours % 12 || 12;
            return String(twelveHour) + ":" + String(minutes).padStart(2, "0") + " " + period;
        }

        function formatTimeValue(totalMinutes) {
            const hours = Math.floor(totalMinutes / 60);
            const minutes = totalMinutes % 60;
            return String(hours).padStart(2, "0") + ":" + String(minutes).padStart(2, "0");
        }

        function rebuildTimeOptions() {
            const preservedValue = timeSelect.value || timeSelect.dataset.selectedTime || "";
            const dateError = getUnavailableDateMessage(dateInput.value);
            const clinicHours = dateError ? null : getClinicHours(dateInput.value);

            timeSelect.innerHTML = "";
            const placeholder = document.createElement("option");
            placeholder.value = "";
            placeholder.textContent = "Select time slot";
            timeSelect.appendChild(placeholder);

            if (!clinicHours) {
                timeSelect.value = "";
                timeSelect.disabled = true;
                timeSelect.dataset.selectedTime = "";
                return;
            }

            const openingParts = clinicHours.opening.split(":");
            const closingParts = clinicHours.closing.split(":");
            const openingMinutes = Number(openingParts[0]) * 60 + Number(openingParts[1]);
            const closingMinutes = Number(closingParts[0]) * 60 + Number(closingParts[1]);

            for (let totalMinutes = openingMinutes; totalMinutes <= closingMinutes; totalMinutes += 15) {
                const option = document.createElement("option");
                option.value = formatTimeValue(totalMinutes);
                option.textContent = formatTimeLabel(totalMinutes);
                timeSelect.appendChild(option);
            }

            timeSelect.disabled = false;

            if (preservedValue && Array.from(timeSelect.options).some(function (option) { return option.value === preservedValue; })) {
                timeSelect.value = preservedValue;
            } else {
                timeSelect.value = "";
            }

            timeSelect.dataset.selectedTime = timeSelect.value;
        }

        function validateDate() {
            const error = getUnavailableDateMessage(dateInput.value);
            dateMessage.textContent = error;
            dateInput.setCustomValidity(error);
            return error === "";
        }

        function getClinicHoursMessage(value) {
            if (!value || !timeSelect.value) {
                return "";
            }

            const clinicHours = getClinicHours(value);
            if (!clinicHours) {
                return "The clinic is closed for the selected date. Please choose another date.";
            }

            if (!Array.from(timeSelect.options).some(function (option) { return option.value === timeSelect.value; })) {
                return "Please select an available surgery time slot.";
            }

            return "";
        }

        function validateTime() {
            const error = getClinicHoursMessage(dateInput.value);
            timeMessage.textContent = error;
            timeSelect.setCustomValidity(error);
            return error === "";
        }

        dateInput.addEventListener("change", function () {
            rebuildTimeOptions();
            validateDate();
            validateTime();
        });

        timeSelect.addEventListener("change", function () {
            timeSelect.dataset.selectedTime = timeSelect.value;
            validateTime();
        });

        form.addEventListener("submit", function (event) {
            const isDateValid = validateDate();
            rebuildTimeOptions();
            const isTimeValid = validateTime();

            if (!isDateValid || !isTimeValid) {
                event.preventDefault();

                if (!isDateValid) {
                    dateInput.reportValidity();
                } else {
                    timeSelect.reportValidity();
                }
            }
        });

        rebuildTimeOptions();
        validateDate();
        validateTime();
    });
}

window.setupAppointmentTimeSelectors = setupAppointmentTimeSelectors;
