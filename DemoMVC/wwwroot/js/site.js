(() => {
    const toggle = document.querySelector("[data-sidebar-toggle]");
    const backdrop = document.getElementById("sidebarBackdrop");

    if (!toggle || !backdrop) {
        return;
    }

    const isDesktop = () => window.matchMedia("(min-width: 992px)").matches;

    const updateToggleState = () => {
        const expanded = isDesktop()
            ? !document.body.classList.contains("sidebar-collapsed")
            : document.body.classList.contains("sidebar-open");

        toggle.setAttribute("aria-expanded", String(expanded));
        toggle.setAttribute(
            "aria-label",
            expanded ? "Thu gọn thanh điều hướng" : "Mở thanh điều hướng");
    };

    const closeMobileSidebar = () => {
        document.body.classList.remove("sidebar-open");
        updateToggleState();
    };

    toggle.addEventListener("click", () => {
        if (isDesktop()) {
            document.body.classList.toggle("sidebar-collapsed");
        } else {
            document.body.classList.toggle("sidebar-open");
        }

        updateToggleState();
    });

    backdrop.addEventListener("click", closeMobileSidebar);

    document.addEventListener("keydown", event => {
        if (event.key === "Escape") {
            closeMobileSidebar();
        }
    });

    document.querySelectorAll(".sidebar-link").forEach(link => {
        link.addEventListener("click", () => {
            if (!isDesktop()) {
                closeMobileSidebar();
            }
        });
    });

    window.addEventListener("resize", () => {
        if (isDesktop()) {
            document.body.classList.remove("sidebar-open");
        }

        updateToggleState();
    });

    updateToggleState();
})();
