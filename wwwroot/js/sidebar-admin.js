document.addEventListener("DOMContentLoaded", function () {
    const menu = document.querySelector(".employees-menu");

    if (!menu) {
        return;
    }

    const toggle = menu.querySelector(".employees-menu-toggle");
    const submenu = menu.querySelector(".submenu");
    const arrow = menu.querySelector(".menu-arrow");
    const storageKey = "sidebar-employees-menu-expanded";

    if (!toggle || !submenu) {
        return;
    }

    let isExpanded = true;

    try {
        const savedState = localStorage.getItem(storageKey);

        if (savedState !== null) {
            isExpanded = savedState === "true";
        }
    } catch (error) {
        // Mantém o submenu aberto caso o navegador bloqueie o localStorage.
    }

    function updateMenuState(expanded) {
        toggle.setAttribute("aria-expanded", String(expanded));
        submenu.hidden = !expanded;

        if (arrow) {
            arrow.classList.toggle("bi-chevron-up", expanded);
            arrow.classList.toggle("bi-chevron-down", !expanded);
        }
    }

    updateMenuState(isExpanded);

    toggle.addEventListener("click", function (event) {
        event.preventDefault();
        isExpanded = !isExpanded;
        updateMenuState(isExpanded);

        try {
            localStorage.setItem(storageKey, String(isExpanded));
        } catch (error) {
            // A interação continua funcionando mesmo sem persistência.
        }
    });
});
