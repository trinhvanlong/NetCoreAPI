(() => {
    const root = document.querySelector("[data-crud-root]");
    if (!root) return;

    const modalElement = root.querySelector("#crudModal");
    const modalContent = root.querySelector("#crudModalContent");
    const listElement = root.querySelector("#crud-list");
    const toastContainer = root.querySelector("#crudToastContainer");
    const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
    let activeLoad = 0;

    const showToast = (message, isError = false) => {
        const toast = document.createElement("div");
        toast.className = `toast align-items-center border-0 ${isError ? "text-bg-danger" : "text-bg-success"}`;
        toast.setAttribute("role", "status");
        toast.setAttribute("aria-live", "polite");
        toast.setAttribute("aria-atomic", "true");

        const row = document.createElement("div");
        row.className = "d-flex";
        const body = document.createElement("div");
        body.className = "toast-body";
        body.textContent = message;
        const close = document.createElement("button");
        close.type = "button";
        close.className = "btn-close btn-close-white me-2 m-auto";
        close.setAttribute("data-bs-dismiss", "toast");
        close.setAttribute("aria-label", "Đóng");

        row.append(body, close);
        toast.append(row);
        toastContainer.append(toast);
        toast.addEventListener("hidden.bs.toast", () => toast.remove(), { once: true });
        bootstrap.Toast.getOrCreateInstance(toast, { delay: 4000 }).show();
    };

    const getErrorMessage = (status, payload) => {
        if (payload && typeof payload.message === "string") return payload.message;
        if (status === 401) return "Vui lòng đăng nhập để tiếp tục.";
        if (status === 403) return "Bạn không có quyền thực hiện thao tác này.";
        if (status === 404) return "Dữ liệu không còn tồn tại hoặc không tìm thấy.";
        if (status === 400) return "Yêu cầu không hợp lệ hoặc mã bảo vệ đã hết hạn. Vui lòng tải lại trang.";
        if (status === 409) return "Dữ liệu vừa thay đổi. Vui lòng tải lại và thử lại.";
        if (status === 503) return "Không thể kết nối cơ sở dữ liệu. Vui lòng thử lại sau.";
        return "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại.";
    };

    const getCaughtErrorMessage = (error, fallback) =>
        error instanceof TypeError || error instanceof SyntaxError
            ? fallback
            : error.message || fallback;

    const showValidationErrors = (form, errors) => {
        const summary = form.querySelector("[data-valmsg-summary=true], .validation-summary-valid, .validation-summary-errors");
        const messages = [];

        for (const error of errors || []) {
            if (!error || typeof error.message !== "string") continue;
            messages.push(error.message);
            if (!error.field) continue;

            const fieldName = error.field.split(".").pop().toLowerCase();
            const fieldMessage = Array.from(form.querySelectorAll("[data-valmsg-for]"))
                .find(element => element.dataset.valmsgFor.toLowerCase() === fieldName);
            if (fieldMessage) {
                fieldMessage.textContent = error.message;
                fieldMessage.classList.remove("field-validation-valid");
                fieldMessage.classList.add("field-validation-error");
            }

            const input = Array.from(form.elements)
                .find(element => element.name && element.name.toLowerCase() === fieldName);
            if (input) input.classList.add("is-invalid");
        }

        if (summary && messages.length) {
            summary.classList.remove("validation-summary-valid");
            summary.classList.add("validation-summary-errors");
            summary.replaceChildren();
            const list = document.createElement("ul");
            for (const message of messages) {
                const item = document.createElement("li");
                item.textContent = message;
                list.append(item);
            }
            summary.append(list);
        }
    };

    const parseJson = async response => {
        const contentType = response.headers.get("content-type") || "";
        if (!contentType.includes("application/json")) return null;
        return response.json();
    };

    const openModal = async url => {
        const currentLoad = ++activeLoad;
        modalContent.innerHTML = `
            <div class="modal-body text-center py-5" role="status">
                <span class="spinner-border text-primary" aria-hidden="true"></span>
                <span class="visually-hidden">Đang tải...</span>
            </div>`;
        modal.show();

        try {
            const response = await fetch(url, {
                headers: {
                    "X-Requested-With": "XMLHttpRequest",
                    "Accept": "text/html, application/json"
                },
                credentials: "same-origin"
            });
            const payload = await parseJson(response);
            if (!response.ok) throw new Error(getErrorMessage(response.status, payload));
            const html = payload === null ? await response.text() : "";
            if (currentLoad !== activeLoad) return;
            if (payload !== null) throw new Error(getErrorMessage(response.status, payload));

            modalContent.innerHTML = html;
            const form = modalContent.querySelector("[data-crud-form]");
            if (form && window.jQuery && jQuery.validator && jQuery.validator.unobtrusive) {
                jQuery.validator.unobtrusive.parse(form);
            }
        } catch (error) {
            if (currentLoad !== activeLoad) return;
            modal.hide();
            showToast(getCaughtErrorMessage(error, "Không thể tải dữ liệu. Vui lòng thử lại."), true);
        }
    };

    const refreshList = async () => {
        const response = await fetch(window.location.href, {
            headers: {
                "X-Requested-With": "XMLHttpRequest",
                "Accept": "text/html"
            },
            credentials: "same-origin"
        });
        if (!response.ok) {
            const payload = await parseJson(response);
            throw new Error(getErrorMessage(response.status, payload));
        }

        const html = await response.text();
        const documentFragment = new DOMParser().parseFromString(html, "text/html");
        const updatedList = documentFragment.querySelector("#crud-list");
        if (!updatedList) throw new Error("Không thể cập nhật danh sách.");
        listElement.replaceChildren(...updatedList.childNodes);

        const total = listElement.querySelector("[data-total-items]")?.dataset.totalItems;
        const summary = root.querySelector(".list-summary strong");
        if (summary && total !== undefined) summary.textContent = total;
    };

    root.addEventListener("click", event => {
        const trigger = event.target.closest("[data-crud-url]");
        if (!trigger || !root.contains(trigger)) return;
        event.preventDefault();
        openModal(trigger.dataset.crudUrl);
    });

    root.addEventListener("submit", async event => {
        const form = event.target.closest("form[data-crud-form]");
        if (!form || !root.contains(form) || event.defaultPrevented) return;
        event.preventDefault();

        const submitButton = form.querySelector('button[type="submit"]');
        const spinner = submitButton?.querySelector(".spinner-border");
        if (submitButton) submitButton.disabled = true;
        spinner?.classList.remove("d-none");

        try {
            const response = await fetch(form.action, {
                method: "POST",
                body: new FormData(form),
                headers: {
                    "X-Requested-With": "XMLHttpRequest",
                    "Accept": "application/json"
                },
                credentials: "same-origin"
            });
            const payload = await parseJson(response);
            if (!response.ok || !payload?.success) {
                if (payload?.errors) showValidationErrors(form, payload.errors);
                throw new Error(getErrorMessage(response.status, payload));
            }

            modal.hide();
            const successMessage = payload.message || "Thao tác thành công.";
            try {
                await refreshList();
            } catch (error) {
                showToast(`${successMessage} Tuy nhiên, không thể cập nhật danh sách. Vui lòng tải lại trang.`, true);
                return;
            }
            showToast(successMessage);
        } catch (error) {
            showToast(getCaughtErrorMessage(error, "Không thể hoàn tất thao tác. Vui lòng thử lại."), true);
        } finally {
            if (submitButton) submitButton.disabled = false;
            spinner?.classList.add("d-none");
        }
    });

    modalElement.addEventListener("hidden.bs.modal", () => {
        activeLoad++;
        modalContent.replaceChildren();
    });

    const url = new URL(window.location.href);
    if (url.searchParams.get("openCreate") === "true") {
        url.searchParams.delete("openCreate");
        window.history.replaceState({}, "", url);
        openModal(root.dataset.createUrl);
    }
})();
