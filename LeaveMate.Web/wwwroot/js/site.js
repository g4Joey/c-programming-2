// LeaveMate — lightweight UI behaviour (no framework dependency).
(function () {
    'use strict';

    /* ---- Mobile off-canvas sidebar ---- */
    const sidebar = document.getElementById('sidebar');
    const scrim = document.getElementById('scrim');
    const toggle = document.getElementById('sidebarToggle');

    function openSidebar() {
        sidebar?.classList.add('is-open');
        if (scrim) scrim.hidden = false;
    }
    function closeSidebar() {
        sidebar?.classList.remove('is-open');
        if (scrim) scrim.hidden = true;
    }
    toggle?.addEventListener('click', function () {
        sidebar?.classList.contains('is-open') ? closeSidebar() : openSidebar();
    });
    scrim?.addEventListener('click', closeSidebar);

    /* ---- Dismissible alerts ---- */
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('[data-alert-dismiss]');
        if (btn) btn.closest('.alert')?.remove();
    });

    /* ---- Modals ---- */
    function openModal(id) {
        const modal = document.getElementById(id);
        if (!modal) return;
        modal.hidden = false;
        document.body.classList.add('has-modal');
        modal.querySelector('.btn, [data-modal-close]')?.focus();
    }
    function closeModal(modal) {
        modal.hidden = true;
        if (!document.querySelector('.modal:not([hidden])')) {
            document.body.classList.remove('has-modal');
        }
    }

    document.addEventListener('click', function (e) {
        const opener = e.target.closest('[data-modal-open]');
        if (opener) {
            e.preventDefault();
            openModal(opener.getAttribute('data-modal-open'));
            return;
        }
        const closer = e.target.closest('[data-modal-close]');
        if (closer) {
            closeModal(closer.closest('.modal'));
            return;
        }
        // Click on the modal backdrop (outside the dialog) closes it.
        if (e.target.classList.contains('modal')) closeModal(e.target);
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            document.querySelectorAll('.modal:not([hidden])').forEach(closeModal);
            closeSidebar();
        }
    });
})();
