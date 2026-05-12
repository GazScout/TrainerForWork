// Проверка сохранённой темы
(function() {
    const savedTheme = getCookie('theme');
    if (savedTheme === 'dark') {
        document.documentElement.classList.add('dark-theme');
    }
})();

// Переключение темы
function toggleTheme() {
    const html = document.documentElement;
    const isDark = html.classList.toggle('dark-theme');
    setCookie('theme', isDark ? 'dark' : 'light', 365);
    
    // Обновление иконки
    updateThemeIcon(isDark);
}

// Обновление иконки
function updateThemeIcon(isDark) {
    const icon = document.querySelector('.theme-toggle .theme-icon');
    if (icon) {
        icon.textContent = isDark ? '☀️' : '🌙';
    }
}

// Инициализация иконки
document.addEventListener('DOMContentLoaded', function() {
    const isDark = document.documentElement.classList.contains('dark-theme');
    updateThemeIcon(isDark);
});

// Работа с куками
function setCookie(name, value, days) {
    const expires = new Date();
    expires.setTime(expires.getTime() + days * 24 * 60 * 60 * 1000);
    document.cookie = name + '=' + value + ';expires=' + expires.toUTCString() + ';path=/';
}

function getCookie(name) {
    const nameEQ = name + '=';
    const ca = document.cookie.split(';');
    for (let i = 0; i < ca.length; i++) {
        let c = ca[i].trim();
        if (c.indexOf(nameEQ) === 0) return c.substring(nameEQ.length, c.length);
    }
    return null;
}