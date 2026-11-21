// Uses the native <dialog> element, which provides the focus trap and inert background.
(function () {
    const returnFocus = [];

    window.taskTrackerDialog = {
        open(dialog, component) {
            returnFocus.push(document.activeElement);

            // Blazor owns whether the dialog is open, so Escape asks it to close.
            dialog.addEventListener('cancel', event => {
                event.preventDefault();
                component.invokeMethodAsync('RequestClose');
            });

            dialog.showModal();
            dialog.querySelector('.modal-body input, .modal-body select, .modal-body textarea, .modal-body button')?.focus();
        },

        restoreFocus() {
            const element = returnFocus.pop();
            if (element?.isConnected) {
                element.focus();
            }
        }
    };
})();
