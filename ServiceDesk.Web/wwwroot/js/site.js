// IT Service Desk Copilot - Main Client JavaScript
(function () {
    'use strict';

    function initTheme() {
        var toggleBtn = document.getElementById('themeToggleBtn');
        var topToggleBtn = document.getElementById('themeToggleTop');
        var themeIcon = document.getElementById('themeIcon');
        var topThemeIcon = topToggleBtn ? topToggleBtn.querySelector('i') : null;
        if (!toggleBtn && !topToggleBtn) return;

        function applyThemeUI(theme) {
            var isDark = theme === 'dark';
            if (theme === 'dark') {
                if (themeIcon) { themeIcon.className = 'bi bi-sun-fill'; themeIcon.style.color = '#f59e0b'; }
                if (topThemeIcon) { topThemeIcon.className = 'bi bi-sun-fill'; topThemeIcon.style.color = '#f59e0b'; }
            } else {
                if (themeIcon) { themeIcon.className = 'bi bi-moon-stars-fill'; themeIcon.style.color = '#4f46e5'; }
                if (topThemeIcon) { topThemeIcon.className = 'bi bi-moon-stars-fill'; topThemeIcon.style.color = '#4f46e5'; }
            }
            [toggleBtn, topToggleBtn].filter(Boolean).forEach(function (button) {
                button.setAttribute('title', isDark ? 'Switch to Light Mode' : 'Switch to Dark Mode');
                button.setAttribute('aria-label', isDark ? 'Switch to Light Mode' : 'Switch to Dark Mode');
            });
        }

        // Check current theme
        var currentTheme = document.documentElement.getAttribute('data-theme') || 'light';
        applyThemeUI(currentTheme);

        function toggleTheme() {
            var active = document.documentElement.getAttribute('data-theme') || 'light';
            var nextTheme = active === 'dark' ? 'light' : 'dark';
            document.documentElement.setAttribute('data-theme', nextTheme);
            localStorage.setItem('sdesk_theme', nextTheme);
            applyThemeUI(nextTheme);
        }

        if (toggleBtn) toggleBtn.addEventListener('click', toggleTheme);
        if (topToggleBtn) topToggleBtn.addEventListener('click', toggleTheme);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initTheme);
    } else {
        initTheme();
    }
})();

// ─── Speech-to-Text Helper ───────────────────────────────────────────────────
(function () {
    'use strict';

    /**
     * SpeechInputHelper: attaches microphone speech-to-text to a button + target input/textarea.
     *
     * @param {HTMLElement} micBtn      - The microphone toggle button element.
     * @param {HTMLElement} targetInput - The input or textarea that receives transcribed text.
     * @param {Object}      [options]
     * @param {string}      [options.lang]                 - BCP-47 language tag (defaults to browser language or 'en-US').
     * @param {boolean}     [options.continuous=false]     - Single-shot recording for input fields.
     * @param {boolean}     [options.interimResults=true]  - Show partial results while speaking.
     * @param {HTMLElement} [options.statusEl]             - Optional status text element.
     */
    function SpeechInputHelper(micBtn, targetInput, options) {
        var opts = Object.assign({
            lang: navigator.language || 'en-US',
            continuous: false,
            interimResults: true
        }, options || {});

        var SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;

        if (!SpeechRecognition) {
            // Unsupported browser — hide mic button
            if (micBtn) {
                micBtn.style.display = 'none';
                micBtn.setAttribute('aria-hidden', 'true');
            }
            return;
        }

        var recognition = null;
        var isListening = false;
        var savedBase = '';

        function setStatus(msg, isError) {
            if (opts.statusEl) {
                opts.statusEl.textContent = msg || '';
                opts.statusEl.style.color = isError ? '#ef4444' : '';
            }
        }

        function createRecognition() {
            var rec = new SpeechRecognition();
            rec.lang = opts.lang || navigator.language || 'en-US';
            rec.continuous = opts.continuous;
            rec.interimResults = opts.interimResults;

            rec.onstart = function () {
                isListening = true;
                micBtn.classList.add('mic-recording');
                micBtn.setAttribute('aria-pressed', 'true');
                micBtn.setAttribute('title', 'Listening... Click to stop');
                micBtn.setAttribute('aria-label', 'Stop listening');
                setStatus('Listening… Speak into your microphone');
            };

            rec.onresult = function (e) {
                var finalTranscript = '';
                var interimTranscript = '';

                for (var i = 0; i < e.results.length; i++) {
                    var transcript = e.results[i][0].transcript;
                    if (e.results[i].isFinal) {
                        finalTranscript += transcript;
                    } else {
                        interimTranscript += transcript;
                    }
                }

                var combined = (finalTranscript + interimTranscript).trim();
                if (combined) {
                    var newText = savedBase ? (savedBase.trim() + ' ' + combined) : combined;
                    targetInput.value = newText;

                    // Trigger input & change events for auto-resize and reactive listeners
                    targetInput.dispatchEvent(new Event('input', { bubbles: true }));
                    targetInput.dispatchEvent(new Event('change', { bubbles: true }));
                    if (window.jQuery) {
                        window.jQuery(targetInput).trigger('input');
                    }
                }
            };

            rec.onerror = function (e) {
                console.warn('[SpeechRecognition] Error:', e.error);
                isListening = false;
                micBtn.classList.remove('mic-recording');
                micBtn.setAttribute('aria-pressed', 'false');

                if (e.error === 'not-allowed' || e.error === 'service-not-allowed') {
                    setStatus('Microphone access denied. Check browser permissions.', true);
                } else if (e.error === 'no-speech') {
                    setStatus('No speech detected. Click mic to try again.', true);
                } else if (e.error === 'network') {
                    setStatus('Network error during speech recognition.', true);
                } else {
                    setStatus('Speech recognition error: ' + e.error, true);
                }
            };

            rec.onend = function () {
                isListening = false;
                micBtn.classList.remove('mic-recording');
                micBtn.setAttribute('aria-pressed', 'false');
                micBtn.setAttribute('title', 'Start voice input');
                micBtn.setAttribute('aria-label', 'Start voice input');
                setTimeout(function () {
                    if (!isListening && opts.statusEl && opts.statusEl.textContent.startsWith('Listening')) {
                        setStatus('');
                    }
                }, 2000);
            };

            return rec;
        }

        micBtn.setAttribute('type', 'button');
        micBtn.setAttribute('aria-pressed', 'false');
        micBtn.setAttribute('title', 'Start voice input');
        micBtn.setAttribute('aria-label', 'Start voice input');

        micBtn.addEventListener('click', function (e) {
            e.preventDefault();
            if (isListening) {
                if (recognition) {
                    try { recognition.stop(); } catch (err) { }
                }
                isListening = false;
                micBtn.classList.remove('mic-recording');
                micBtn.setAttribute('aria-pressed', 'false');
                setStatus('');
            } else {
                savedBase = targetInput.value || '';
                try {
                    recognition = createRecognition();
                    recognition.start();
                } catch (err) {
                    console.error('[SpeechRecognition] Start failed:', err);
                    setStatus('Could not start microphone.', true);
                }
            }
        });
    }

    // Expose globally so page scripts can initialize on demand
    window.SpeechInputHelper = SpeechInputHelper;

    // Auto-init any elements with data-speech-target attribute on DOMContentLoaded
    function autoInit() {
        document.querySelectorAll('[data-speech-target]').forEach(function (btn) {
            var targetId = btn.getAttribute('data-speech-target');
            var targetEl = document.getElementById(targetId);
            if (!targetEl) return;
            var statusId = btn.getAttribute('data-speech-status');
            var statusEl = statusId ? document.getElementById(statusId) : null;
            new SpeechInputHelper(btn, targetEl, { statusEl: statusEl });
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', autoInit);
    } else {
        autoInit();
    }
})();

