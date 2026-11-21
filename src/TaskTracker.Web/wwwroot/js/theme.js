(function () {
    const storageKey = 'tasktracker.theme';
    const systemDark = window.matchMedia('(prefers-color-scheme: dark)');

    function read() {
        try {
            return localStorage.getItem(storageKey) || 'dark';
        } catch {
            return 'dark';
        }
    }

    function apply(preference) {
        const theme = preference === 'system' ? (systemDark.matches ? 'dark' : 'light') : preference;
        document.documentElement.dataset.theme = theme;
    }

    systemDark.addEventListener('change', () => {
        if (read() === 'system') {
            apply('system');
        }
    });

    window.taskTrackerTheme = {
        get: read,
        set(preference) {
            try {
                localStorage.setItem(storageKey, preference);
            } catch {
                // The theme still applies for this page when storage is unavailable.
            }
            apply(preference);
        }
    };

    apply(read());
})();
