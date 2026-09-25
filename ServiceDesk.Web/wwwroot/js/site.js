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

    function initSidebarToggle() {
        var shell = document.getElementById('appShell');
        var toggleBtn = document.getElementById('sidebarToggleBtn');
        if (!shell || !toggleBtn) return;

        toggleBtn.addEventListener('click', function () {
            shell.classList.toggle('sidebar-collapsed');
            var isCollapsed = shell.classList.contains('sidebar-collapsed');
            localStorage.setItem('sdesk_sidebar_collapsed', isCollapsed ? 'true' : 'false');
            document.cookie = "sdesk_sidebar_collapsed=" + isCollapsed + "; path=/; max-age=" + (365 * 86400);
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () {
            initTheme();
            initSidebarToggle();
        });
    } else {
        initTheme();
        initSidebarToggle();
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

// ─── Copy Response Helper ───────────────────────────────────────────────────
(function () {
    'use strict';

    /**
     * Copies text to clipboard with fallback for non-HTTPS or legacy environments.
     * @param {string} text
     * @returns {Promise<boolean>}
     */
    function copyToClipboard(text) {
        if (navigator.clipboard && window.isSecureContext) {
            return navigator.clipboard.writeText(text).then(function () {
                return true;
            }).catch(function (err) {
                console.warn('[Copy] Clipboard API failed, attempting fallback:', err);
                return fallbackCopyText(text);
            });
        }
        return Promise.resolve(fallbackCopyText(text));
    }

    function fallbackCopyText(text) {
        try {
            var textArea = document.createElement('textarea');
            textArea.value = text;
            textArea.style.position = 'fixed';
            textArea.style.top = '0';
            textArea.style.left = '-9999px';
            textArea.style.opacity = '0';
            document.body.appendChild(textArea);
            textArea.focus();
            textArea.select();
            var successful = document.execCommand('copy');
            document.body.removeChild(textArea);
            return successful;
        } catch (err) {
            console.error('[Copy] Fallback copy failed:', err);
            return false;
        }
    }

    /**
     * Event Delegation for Copy Buttons (.btn-copy-response)
     */
    function initCopyResponseHandler() {
        document.addEventListener('click', function (e) {
            var btn = e.target.closest('.btn-copy-response');
            if (!btn) return;

            e.preventDefault();

            // Locate parent message container
            var msgContainer = btn.closest('.msg-row, .chat-bubble-assistant, .msg-bubble');
            if (!msgContainer) return;

            // Extract plain text answer from .msg-text-content
            var textEl = msgContainer.querySelector('.msg-text-content');
            var textToCopy = '';

            if (textEl) {
                textToCopy = textEl.innerText || textEl.textContent || '';
            } else {
                // Fallback: clone container and remove metadata/actions
                var clone = msgContainer.cloneNode(true);
                clone.querySelectorAll('.citation-row, .proposal-card, .proposal-banner, .btn-copy-response, .msg-time, .msg-time-row').forEach(function (el) {
                    el.remove();
                });
                textToCopy = clone.innerText || clone.textContent || '';
            }

            textToCopy = textToCopy.trim();
            if (!textToCopy) return;

            // Avoid rapid double-clicking while active
            if (btn.dataset.copying === 'true') return;
            btn.dataset.copying = 'true';

            copyToClipboard(textToCopy).then(function (success) {
                var iconEl = btn.querySelector('i');
                var textSpan = btn.querySelector('.copy-text');

                var origIconClass = iconEl ? iconEl.className : 'bi bi-copy';
                var origText = textSpan ? textSpan.textContent : 'Copy';
                var origTitle = btn.getAttribute('title') || 'Copy response';

                if (success) {
                    btn.classList.add('copied');
                    if (iconEl) iconEl.className = 'bi bi-check2';
                    if (textSpan) textSpan.textContent = 'Copied';
                    btn.setAttribute('title', 'Response copied');
                    btn.setAttribute('aria-label', 'Response copied');

                    announceCopyStatus('Response copied to clipboard.');
                } else {
                    btn.classList.add('copy-failed');
                    if (iconEl) iconEl.className = 'bi bi-x-circle';
                    if (textSpan) textSpan.textContent = 'Failed';
                    btn.setAttribute('title', 'Failed to copy');
                    btn.setAttribute('aria-label', 'Failed to copy');

                    announceCopyStatus('Failed to copy response.');
                }

                setTimeout(function () {
                    btn.classList.remove('copied', 'copy-failed');
                    if (iconEl) iconEl.className = origIconClass;
                    if (textSpan) textSpan.textContent = origText;
                    btn.setAttribute('title', origTitle);
                    btn.setAttribute('aria-label', origTitle);
                    delete btn.dataset.copying;
                }, 2000);
            });
        });
    }

    function announceCopyStatus(message) {
        var statusEl = document.getElementById('globalCopyStatus');
        if (!statusEl) {
            statusEl = document.createElement('span');
            statusEl.id = 'globalCopyStatus';
            statusEl.className = 'visually-hidden';
            statusEl.setAttribute('role', 'status');
            statusEl.setAttribute('aria-live', 'polite');
            statusEl.setAttribute('aria-atomic', 'true');
            document.body.appendChild(statusEl);
        }
        statusEl.textContent = message;
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initCopyResponseHandler);
    } else {
        initCopyResponseHandler();
    }
})();

// ─── Reusable Table & List Paginator ─────────────────────────────────────────
(function () {
    'use strict';

    function TablePaginator(container, options) {
        if (!container) return;
        var self = this;

        this.container = typeof container === 'string' ? document.querySelector(container) : container;
        if (!this.container) return;

        this.options = Object.assign({
            pageSize: 10,
            itemSelector: null,
            wrapperClass: 'table-pagination-wrapper',
            onPageChange: null
        }, options || {});

        // Detect default item selector
        if (!this.options.itemSelector) {
            if (this.container.tagName === 'TABLE') {
                this.options.itemSelector = 'tbody > tr:not(.no-paginate)';
            } else {
                this.options.itemSelector = '> *:not(.no-paginate)';
            }
        }

        var dataPageSize = parseInt(this.container.getAttribute('data-page-size') || this.container.getAttribute('data-paginate'), 10);
        if (!isNaN(dataPageSize) && dataPageSize > 0) {
            this.options.pageSize = dataPageSize;
        }

        this.currentPage = 1;
        this.controlsEl = null;

        this.init();
    }

    TablePaginator.prototype.getItems = function () {
        if (this.container.tagName === 'TABLE') {
            return Array.from(this.container.querySelectorAll('tbody > tr:not(.no-paginate)'));
        }
        if (this.options.itemSelector && this.options.itemSelector !== '> *:not(.no-paginate)') {
            try {
                return Array.from(this.container.querySelectorAll(this.options.itemSelector));
            } catch (e) {
                // Ignore selector error and fall back to children
            }
        }
        return Array.from(this.container.children).filter(function (child) {
            return !child.classList.contains('no-paginate') && !child.classList.contains('table-pagination-wrapper');
        });
    };

    TablePaginator.prototype.update = function (resetPage) {
        if (resetPage) {
            this.currentPage = 1;
        }

        var allItems = this.getItems();
        var candidateItems = allItems.filter(function (item) {
            return item.dataset.filteredOut !== 'true';
        });

        var totalItems = candidateItems.length;
        var pageSize = this.options.pageSize;
        var totalPages = Math.ceil(totalItems / pageSize) || 1;

        if (this.currentPage > totalPages) {
            this.currentPage = totalPages;
        }
        if (this.currentPage < 1) {
            this.currentPage = 1;
        }

        var startIndex = (this.currentPage - 1) * pageSize;
        var endIndex = startIndex + pageSize;

        allItems.forEach(function (item) {
            item.style.display = 'none';
            delete item.dataset.paginatedVisible;
        });

        candidateItems.forEach(function (item, idx) {
            if (idx >= startIndex && idx < endIndex) {
                item.style.display = '';
                item.dataset.paginatedVisible = 'true';
            } else {
                item.style.display = 'none';
            }
        });

        this.renderControls(totalItems, startIndex, Math.min(endIndex, totalItems), totalPages);

        if (typeof this.options.onPageChange === 'function') {
            this.options.onPageChange(this.currentPage, totalPages, totalItems);
        }
    };

    TablePaginator.prototype.renderControls = function (totalItems, startItem, endItem, totalPages) {
        var self = this;

        if (!this.controlsEl) {
            var parent = this.container.closest('.table-responsive') || this.container.parentElement || this.container;
            var existing = parent.parentElement ? parent.parentElement.querySelector('.' + this.options.wrapperClass) : null;
            if (existing) {
                this.controlsEl = existing;
            } else {
                this.controlsEl = document.createElement('div');
                this.controlsEl.className = this.options.wrapperClass + ' d-flex flex-wrap align-items-center justify-content-between gap-2 pt-3 pb-2 px-3 border-top';
                parent.parentElement.insertBefore(this.controlsEl, parent.nextSibling);
            }
        }

        if (totalItems === 0) {
            this.controlsEl.innerHTML = `
                <div class="pagination-info text-muted small">Showing 0 entries</div>
                <nav aria-label="Table pagination">
                    <ul class="pagination pagination-sm m-0 gap-1">
                        <li class="page-item disabled"><button type="button" class="page-link" disabled aria-label="Previous page"><i class="bi bi-chevron-left"></i> Previous</button></li>
                        <li class="page-item disabled"><button type="button" class="page-link" disabled aria-label="Next page">Next <i class="bi bi-chevron-right"></i></button></li>
                    </ul>
                </nav>`;
            return;
        }

        var rangeStart = startItem + 1;
        var rangeEnd = endItem;

        var infoText = `Showing <span class="fw-semibold" style="color:var(--text-heading);">${rangeStart}</span>–<span class="fw-semibold" style="color:var(--text-heading);">${rangeEnd}</span> of <span class="fw-semibold" style="color:var(--text-heading);">${totalItems}</span> entries`;

        var pagesHtml = '';

        var prevDisabled = this.currentPage === 1 ? 'disabled' : '';
        pagesHtml += `<li class="page-item ${prevDisabled}"><button type="button" class="page-link btn-page-prev" ${prevDisabled} aria-label="Previous page"><i class="bi bi-chevron-left"></i> Previous</button></li>`;

        var maxVisibleButtons = 5;
        var startPage = Math.max(1, this.currentPage - 2);
        var endPage = Math.min(totalPages, startPage + maxVisibleButtons - 1);

        if (endPage - startPage + 1 < maxVisibleButtons) {
            startPage = Math.max(1, endPage - maxVisibleButtons + 1);
        }

        if (startPage > 1) {
            pagesHtml += `<li class="page-item"><button type="button" class="page-link btn-page-num" data-page="1">1</button></li>`;
            if (startPage > 2) {
                pagesHtml += `<li class="page-item disabled"><span class="page-link px-2">…</span></li>`;
            }
        }

        for (var p = startPage; p <= endPage; p++) {
            var activeClass = p === this.currentPage ? 'active' : '';
            var ariaCurrent = p === this.currentPage ? 'aria-current="page"' : '';
            pagesHtml += `<li class="page-item ${activeClass}"><button type="button" class="page-link btn-page-num" data-page="${p}" ${ariaCurrent}>${p}</button></li>`;
        }

        if (endPage < totalPages) {
            if (endPage < totalPages - 1) {
                pagesHtml += `<li class="page-item disabled"><span class="page-link px-2">…</span></li>`;
            }
            pagesHtml += `<li class="page-item"><button type="button" class="page-link btn-page-num" data-page="${totalPages}">${totalPages}</button></li>`;
        }

        var nextDisabled = this.currentPage === totalPages ? 'disabled' : '';
        pagesHtml += `<li class="page-item ${nextDisabled}"><button type="button" class="page-link btn-page-next" ${nextDisabled} aria-label="Next page">Next <i class="bi bi-chevron-right"></i></button></li>`;

        this.controlsEl.innerHTML = `
            <div class="pagination-info text-muted small" style="font-size:0.8rem;font-weight:500;">${infoText}</div>
            <nav aria-label="Table pagination">
                <ul class="pagination pagination-sm m-0 gap-1">${pagesHtml}</ul>
            </nav>`;

        var prevBtn = this.controlsEl.querySelector('.btn-page-prev');
        if (prevBtn && !prevDisabled) {
            prevBtn.onclick = function () {
                if (self.currentPage > 1) {
                    self.currentPage--;
                    self.update();
                }
            };
        }

        var nextBtn = this.controlsEl.querySelector('.btn-page-next');
        if (nextBtn && !nextDisabled) {
            nextBtn.onclick = function () {
                if (self.currentPage < totalPages) {
                    self.currentPage++;
                    self.update();
                }
            };
        }

        this.controlsEl.querySelectorAll('.btn-page-num').forEach(function (btn) {
            btn.onclick = function () {
                var p = parseInt(btn.getAttribute('data-page'), 10);
                if (!isNaN(p) && p !== self.currentPage) {
                    self.currentPage = p;
                    self.update();
                }
            };
        });
    };

    TablePaginator.prototype.init = function () {
        this.update(true);
    };

    window.TablePaginator = TablePaginator;

    function autoInitPaginators() {
        document.querySelectorAll('table[data-paginate], [data-paginate]').forEach(function (el) {
            if (!el.dataset.paginatorInitialized) {
                el.dataset.paginatorInitialized = 'true';
                var pageSize = parseInt(el.getAttribute('data-paginate'), 10);
                if (isNaN(pageSize) || pageSize <= 0) pageSize = 10;
                var paginator = new TablePaginator(el, { pageSize: pageSize });
                el.tablePaginator = paginator;
            }
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', autoInitPaginators);
    } else {
        autoInitPaginators();
    }
})();

// ─── Knowledge Base Client Module ─────────────────────────────────────────────
(function () {
    'use strict';

    var lastTriggerElement = null;

    /**
     * Opens a document in the polished modal viewer.
     * @param {string} docId
     * @param {HTMLElement} [triggerEl]
     */
    function openKnowledgeDocument(docId, triggerEl) {
        if (!docId) return;

        lastTriggerElement = triggerEl || document.activeElement;

        var drawer = document.getElementById('kbDocumentDrawer');
        var backdrop = document.getElementById('kbDrawerBackdrop');

        if (drawer && backdrop) {
            var drawerTitle = document.getElementById('kbDrawerTitle');
            var drawerSection = document.getElementById('kbDrawerSection');
            var drawerVersion = document.getElementById('kbDrawerVersion');
            var drawerUpdated = document.getElementById('kbDrawerUpdated');
            var drawerSpinner = document.getElementById('kbDrawerSpinner');
            var drawerError = document.getElementById('kbDrawerError');
            var drawerErrMsg = document.getElementById('kbDrawerErrorMessage');
            var drawerContent = document.getElementById('kbDrawerContent');

            if (drawerTitle) drawerTitle.textContent = 'Loading Document...';
            if (drawerSpinner) drawerSpinner.style.display = 'block';
            if (drawerError) drawerError.style.display = 'none';
            if (drawerContent) { drawerContent.style.display = 'none'; drawerContent.innerHTML = ''; }

            drawer.classList.add('active');
            backdrop.classList.add('active');
            drawer.setAttribute('aria-hidden', 'false');

            function closeDrawer() {
                drawer.classList.remove('active');
                backdrop.classList.remove('active');
                drawer.setAttribute('aria-hidden', 'true');
                if (lastTriggerElement && typeof lastTriggerElement.focus === 'function') {
                    lastTriggerElement.focus();
                }
            }

            var btnClose = document.getElementById('kbDrawerBtnClose');
            var btnCloseFooter = document.getElementById('kbDrawerBtnCloseFooter');
            if (btnClose) btnClose.onclick = closeDrawer;
            if (btnCloseFooter) btnCloseFooter.onclick = closeDrawer;
            backdrop.onclick = closeDrawer;

            fetch('/Knowledge/Document/' + encodeURIComponent(docId), {
                headers: { 'Accept': 'application/json' }
            })
            .then(function (res) {
                if (!res.ok) throw new Error('HTTP ' + res.status);
                return res.json();
            })
            .then(function (res) {
                if (drawerSpinner) drawerSpinner.style.display = 'none';
                if (res && res.success && res.data) {
                    var doc = res.data.document;
                    var renderedHtml = res.data.renderedHtml;

                    if (drawerTitle) drawerTitle.textContent = doc.documentName || doc.id;
                    if (drawerSection) drawerSection.textContent = doc.section || 'General';
                    if (drawerVersion) drawerVersion.textContent = 'v' + (doc.version || '1.0');
                    if (drawerUpdated && doc.lastModified) {
                        var dt = new Date(doc.lastModified);
                        drawerUpdated.textContent = isNaN(dt.getTime()) ? doc.lastModified : dt.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
                    }
                    if (drawerContent) {
                        drawerContent.innerHTML = renderedHtml;
                        drawerContent.style.display = 'block';
                        drawerContent.dataset.rawText = drawerContent.innerText || drawerContent.textContent || '';
                    }
                } else {
                    throw new Error((res && res.message) ? res.message : 'Failed to retrieve document details.');
                }
            })
            .catch(function (err) {
                if (drawerSpinner) drawerSpinner.style.display = 'none';
                if (drawerError) {
                    if (drawerErrMsg) drawerErrMsg.textContent = err.message || 'Azure Blob Storage knowledge source is currently unavailable.';
                    drawerError.style.display = 'block';
                }
            });

            var copyBtn = document.getElementById('kbDrawerBtnCopy');
            if (copyBtn) {
                copyBtn.onclick = function() {
                    if (!drawerContent) return;
                    var text = drawerContent.dataset.rawText || drawerContent.innerText || '';
                    if (navigator.clipboard && window.isSecureContext) {
                        navigator.clipboard.writeText(text).then(function() {
                            var copyTextEl = document.getElementById('kbDrawerCopyText');
                            if (copyTextEl) copyTextEl.textContent = 'Copied!';
                            setTimeout(function() { if (copyTextEl) copyTextEl.textContent = 'Copy'; }, 2000);
                        });
                    }
                };
            }

            var printBtn = document.getElementById('kbDrawerBtnPrint');
            if (printBtn) {
                printBtn.onclick = function() {
                    if (!drawerContent) return;
                    var title = drawerTitle ? drawerTitle.textContent : 'Document';
                    var printWin = window.open('', '_blank', 'width=800,height=600');
                    if (!printWin) return;
                    printWin.document.write('<html><head><title>' + title + '</title></head><body><h1>' + title + '</h1>' + drawerContent.innerHTML + '</body></html>');
                    printWin.document.close();
                    printWin.focus();
                    setTimeout(function() { printWin.print(); printWin.close(); }, 250);
                };
            }

            return;
        }

        var modalEl = document.getElementById('kbDocumentModal');
        if (!modalEl) {
            console.warn('[KnowledgeBase] Document drawer/modal element not found.');
            return;
        }

        // Fetch document detail from dedicated endpoint
        fetch('/Knowledge/Document/' + encodeURIComponent(docId), {
            headers: { 'Accept': 'application/json' }
        })
        .then(function (res) {
            if (!res.ok) {
                return res.json().then(function (data) {
                    throw new Error(data.message || data.error || ('HTTP ' + res.status));
                }).catch(function (e) {
                    throw new Error(e.message || ('HTTP ' + res.status));
                });
            }
            return res.json();
        })
        .then(function (res) {
            if (modalSpinner) modalSpinner.style.display = 'none';

            if (res && res.success && res.data) {
                var doc = res.data.document;
                var renderedHtml = res.data.renderedHtml;

                if (modalTitle) modalTitle.textContent = doc.documentName || doc.id;
                if (modalSection) modalSection.textContent = doc.section || 'General';
                if (modalVersion) modalVersion.textContent = 'v' + (doc.version || '1.0');
                if (modalUpdated && doc.lastModified) {
                    var dt = new Date(doc.lastModified);
                    modalUpdated.textContent = isNaN(dt.getTime()) ? doc.lastModified : dt.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
                }

                if (modalContent) {
                    modalContent.innerHTML = renderedHtml;
                    modalContent.style.display = 'block';
                    modalContent.dataset.rawText = modalContent.innerText || modalContent.textContent || '';
                }
            } else {
                throw new Error((res && res.message) ? res.message : 'Failed to retrieve document details.');
            }
        })
        .catch(function (err) {
            console.error('[KnowledgeBase] Error loading document detail:', err);
            if (modalSpinner) modalSpinner.style.display = 'none';
            if (modalError) {
                if (modalErrMsg) modalErrMsg.textContent = err.message || 'Azure Blob Storage knowledge source is currently unavailable.';
                modalError.style.display = 'block';
            }
        });
    }

    // Expose globally
    window.openKnowledgeDocument = openKnowledgeDocument;

    // Attach copy & print actions inside modal
    function initModalActions() {
        var copyBtn = document.getElementById('kbModalBtnCopy');
        if (copyBtn) {
            copyBtn.addEventListener('click', function () {
                var contentEl = document.getElementById('kbModalContent');
                if (!contentEl) return;

                var textToCopy = contentEl.dataset.rawText || contentEl.innerText || contentEl.textContent || '';
                textToCopy = textToCopy.trim();
                if (!textToCopy) return;

                var copyTextEl = document.getElementById('kbModalCopyText');
                var iconEl = copyBtn.querySelector('i');

                if (navigator.clipboard && window.isSecureContext) {
                    navigator.clipboard.writeText(textToCopy).then(showCopied).catch(fallback);
                } else {
                    fallback();
                }

                function fallback() {
                    try {
                        var ta = document.createElement('textarea');
                        ta.value = textToCopy;
                        ta.style.position = 'fixed';
                        ta.style.left = '-9999px';
                        document.body.appendChild(ta);
                        ta.select();
                        document.execCommand('copy');
                        document.body.removeChild(ta);
                        showCopied();
                    } catch (e) { }
                }

                function showCopied() {
                    if (iconEl) iconEl.className = 'bi bi-check2';
                    if (copyTextEl) copyTextEl.textContent = 'Copied!';
                    copyBtn.classList.add('btn-success');
                    copyBtn.classList.remove('btn-outline-secondary');

                    setTimeout(function () {
                        if (iconEl) iconEl.className = 'bi bi-clipboard me-1';
                        if (copyTextEl) copyTextEl.textContent = 'Copy Content';
                        copyBtn.classList.remove('btn-success');
                        copyBtn.classList.add('btn-outline-secondary');
                    }, 2000);
                }
            });
        }

        var printBtn = document.getElementById('kbModalBtnPrint');
        if (printBtn) {
            printBtn.addEventListener('click', function () {
                var titleEl = document.getElementById('kbModalTitle');
                var contentEl = document.getElementById('kbModalContent');
                if (!contentEl) return;

                var title = titleEl ? titleEl.textContent : 'Document';
                var html = contentEl.innerHTML;

                var printWin = window.open('', '_blank', 'width=800,height=600');
                if (!printWin) return;

                printWin.document.write(`
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <title>${title}</title>
                        <style>
                            body { font-family: system-ui, sans-serif; padding: 24px; color: #111; line-height: 1.6; }
                            h1 { border-bottom: 2px solid #ccc; padding-bottom: 8px; }
                            table { width: 100%; border-collapse: collapse; margin: 16px 0; }
                            th, td { border: 1px solid #ccc; padding: 8px; }
                            pre { background: #f4f4f4; padding: 12px; border-radius: 4px; }
                            code { background: #eee; padding: 2px 4px; }
                        </style>
                    </head>
                    <body>
                        <h1>${title}</h1>
                        <div>${html}</div>
                    </body>
                    </html>
                `);
                printWin.document.close();
                printWin.focus();
                setTimeout(function () {
                    printWin.print();
                    printWin.close();
                }, 250);
            });
        }

        // Restore focus on modal close
        var modalEl = document.getElementById('kbDocumentModal');
        if (modalEl) {
            modalEl.addEventListener('hidden.bs.modal', function () {
                if (lastTriggerElement && typeof lastTriggerElement.focus === 'function') {
                    lastTriggerElement.focus();
                }
            });
        }
    }

    // Init Knowledge Base Widget Data & Search/Filters
    function initKnowledgeBaseWidget() {
        var searchInput = document.getElementById('kbSearchInput');
        var catFilter = document.getElementById('kbCategoryFilter');
        var resetBtn = document.getElementById('btnResetKbFilters');
        var gridContainer = document.getElementById('kbGridContainer');
        var countBadge = document.getElementById('kbCountBadge');
        var emptyState = document.getElementById('kbEmptyState');
        var loadingState = document.getElementById('kbLoadingState');
        var errorState = document.getElementById('kbErrorState');
        var errorMsg = document.getElementById('kbErrorMessage');

        if (!gridContainer) return;

        function applyFilters() {
            var rawQ = (searchInput ? searchInput.value : '').toLowerCase().trim();
            var cat = (catFilter ? catFilter.value : '').toLowerCase().trim();

            var cards = Array.from(gridContainer.querySelectorAll('.kb-card-col'));
            if (cards.length === 0) return;

            var searchTerms = rawQ ? rawQ.split(/\s+/).filter(Boolean) : [];

            function getCardMetaText(card) {
                var title = (card.getAttribute('data-title') || '').toLowerCase();
                var id = (card.getAttribute('data-id') || '').toLowerCase();
                var section = (card.getAttribute('data-section') || '').toLowerCase();
                var combined = title + ' ' + id + ' ' + section;
                var normalized = combined.replace(/[-_&]/g, ' ') + ' ' + combined.replace(/[-_&]/g, '');
                return combined + ' ' + normalized;
            }

            function getCardContentText(card) {
                var content = (card.getAttribute('data-content') || '').toLowerCase();
                var cardText = card.textContent.toLowerCase();
                return content + ' ' + cardText;
            }

            // If user typed search terms, check if any card matches in document name/id/section
            var hasPrimaryMatches = false;
            if (searchTerms.length > 0) {
                hasPrimaryMatches = cards.some(function (card) {
                    var metaText = getCardMetaText(card);
                    return searchTerms.every(function (term) {
                        return metaText.includes(term);
                    });
                });
            }

            var visibleCount = 0;

            cards.forEach(function (card) {
                var section = (card.getAttribute('data-section') || '').toLowerCase();
                var metaText = getCardMetaText(card);
                var contentText = getCardContentText(card);

                var matchesCat = true;
                if (cat) {
                    matchesCat = (section === cat || section.includes(cat) || cat.includes(section));
                }

                var matchesSearch = true;
                if (searchTerms.length > 0) {
                    if (hasPrimaryMatches) {
                        // Strict document name/section/id matching so ONLY that particular document is displayed
                        matchesSearch = searchTerms.every(function (term) {
                            return metaText.includes(term);
                        });
                    } else {
                        // Fallback to content if no title/name match
                        matchesSearch = searchTerms.every(function (term) {
                            return metaText.includes(term) || contentText.includes(term);
                        });
                    }
                }

                if (matchesSearch && matchesCat) {
                    card.style.display = '';
                    card.removeAttribute('data-filtered-out');
                    delete card.dataset.filteredOut;
                    visibleCount++;
                } else {
                    card.style.display = 'none';
                    card.setAttribute('data-filtered-out', 'true');
                    card.dataset.filteredOut = 'true';
                }
            });

            if (countBadge) {
                countBadge.textContent = visibleCount + ' Approved Documents';
            }

            if (emptyState) {
                emptyState.style.display = (visibleCount === 0 && cards.length > 0) ? 'block' : 'none';
            }

            if (gridContainer.tablePaginator) {
                try {
                    gridContainer.tablePaginator.update(true);
                } catch (e) {
                    console.warn('[TablePaginator] Update failed:', e);
                }
            }
        }

        if (searchInput) searchInput.addEventListener('input', applyFilters);
        if (catFilter) catFilter.addEventListener('change', applyFilters);
        if (resetBtn) {
            resetBtn.addEventListener('click', function () {
                if (searchInput) searchInput.value = '';
                if (catFilter) catFilter.value = '';
                applyFilters();
            });
        }

        // If cards aren't pre-rendered by Razor model, load dynamically via AJAX
        var existingCards = gridContainer.querySelectorAll('.kb-card-col');
        if (existingCards.length === 0) {
            if (loadingState) loadingState.style.display = 'block';

            fetch('/Knowledge/GetDocuments', {
                headers: { 'Accept': 'application/json' }
            })
            .then(function (res) {
                if (!res.ok) {
                    return res.json().then(function (data) {
                        throw new Error(data.message || data.error || 'Azure Blob Storage knowledge source is currently unavailable.');
                    }).catch(function (e) {
                        throw new Error(e.message || 'Azure Blob Storage knowledge source is currently unavailable.');
                    });
                }
                return res.json();
            })
            .then(function (res) {
                if (loadingState) loadingState.style.display = 'none';

                if (res && res.success && Array.isArray(res.documents)) {
                    renderDocuments(res.documents);
                } else {
                    throw new Error((res && res.message) ? res.message : 'Azure Blob Storage knowledge source returned invalid data.');
                }
            })
            .catch(function (err) {
                console.error('[KnowledgeBase] Error fetching document list:', err);
                if (loadingState) loadingState.style.display = 'none';
                if (errorState) {
                    if (errorMsg) errorMsg.textContent = err.message || 'Azure Blob Storage knowledge source is currently unavailable. Local file fallback is disabled.';
                    errorState.style.display = 'block';
                }
            });
        } else {
            // Already rendered via Razor, initialize pagination and apply filters
            if (window.TablePaginator && !gridContainer.tablePaginator) {
                gridContainer.tablePaginator = new window.TablePaginator(gridContainer, { pageSize: 10 });
            }
            applyFilters();
        }

        function createDocumentCardElement(doc) {
            var section = doc.section || 'General';
            var catColor = section === 'Security & Access' ? '#ef4444' :
                           section === 'Network & Connectivity' ? '#3b82f6' :
                           section === 'Devices & Hardware' ? '#f59e0b' : '#7c3aed';
            var catIcon = section === 'Security & Access' ? 'bi-shield-lock-fill' :
                          section === 'Network & Connectivity' ? 'bi-wifi' :
                          section === 'Devices & Hardware' ? 'bi-pc-display' : 'bi-file-earmark-text-fill';

            var updatedStr = '—';
            if (doc.lastModified) {
                var dt = new Date(doc.lastModified);
                updatedStr = isNaN(dt.getTime()) ? doc.lastModified : dt.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
            }

            var col = document.createElement('div');
            col.className = 'col-12 col-md-6 col-lg-6 kb-card-col';
            col.setAttribute('data-id', (doc.id || '').toLowerCase());
            col.setAttribute('data-title', (doc.documentName || '').toLowerCase());
            col.setAttribute('data-section', section.toLowerCase());
            col.setAttribute('data-content', (doc.content || '').toLowerCase());

            col.innerHTML = `
                <div class="card h-100 kb-doc-card rounded-3 p-3 transition-all"
                     style="background:var(--bg-surface);border:1px solid var(--border-soft);cursor:pointer;overflow:hidden;box-sizing:border-box;width:100%;"
                     tabindex="0" role="button" aria-label="Open document ${doc.documentName}">
                    <div class="d-flex align-items-start gap-3">
                        <div class="rounded-3 p-2 d-flex align-items-center justify-content-center flex-shrink-0"
                             style="background:rgba(124,58,237,0.1);width:42px;height:42px;">
                            <i class="bi ${catIcon}" style="color:${catColor};font-size:1.25rem;"></i>
                        </div>
                        <div class="flex-grow-1 min-w-0">
                            <div class="mb-1">
                                <h6 class="fw-bold mb-0 text-truncate" style="color:var(--text-heading);font-size:0.95rem;line-height:1.35;" title="${doc.documentName}">
                                    ${doc.documentName}
                                </h6>
                            </div>
                            <div class="d-flex flex-wrap align-items-center gap-2 mb-2" style="font-size:0.78rem;color:var(--text-muted);">
                                <span class="badge bg-primary-subtle text-primary border border-primary-subtle rounded-pill text-truncate" style="max-width:200px;">
                                    <i class="bi bi-folder2-open me-1"></i>${section}
                                </span>
                                <span class="text-nowrap small"><i class="bi bi-clock me-1"></i>${updatedStr}</span>
                            </div>
                            <div class="d-flex flex-wrap align-items-center justify-content-between gap-2 mt-2 pt-2 border-top" style="border-color:var(--border-soft) !important;">
                                <span class="badge bg-success-subtle text-success border border-success-subtle rounded-pill text-nowrap" style="font-size:0.7rem;">
                                    <i class="bi bi-check-circle-fill me-1"></i> Approved &amp; Active
                                </span>
                                <span class="btn btn-sm btn-link p-0 text-decoration-none fw-semibold text-nowrap d-inline-flex align-items-center" style="font-size:0.8rem;color:var(--brand-blue, #2563eb);">
                                    Read Document <i class="bi bi-arrow-right ms-1"></i>
                                </span>
                            </div>
                        </div>
                    </div>
                </div>`;

            var cardEl = col.querySelector('.kb-doc-card');
            cardEl.onclick = function () {
                openKnowledgeDocument(doc.id, cardEl);
            };
            cardEl.onkeydown = function (e) {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    openKnowledgeDocument(doc.id, cardEl);
                }
            };

            return col;
        }

        function renderDocuments(documents) {
            gridContainer.innerHTML = '';

            if (!documents || documents.length === 0) {
                if (emptyState) emptyState.style.display = 'block';
                if (countBadge) countBadge.textContent = '0 Approved Documents';
                return;
            }

            // Populate category dropdown options dynamically
            if (catFilter && catFilter.options.length <= 1) {
                var sections = [];
                documents.forEach(function (d) {
                    if (d.section && !sections.includes(d.section)) sections.push(d.section);
                });
                sections.sort().forEach(function (sec) {
                    var opt = document.createElement('option');
                    opt.value = sec;
                    opt.textContent = sec;
                    catFilter.appendChild(opt);
                });
            }

            documents.forEach(function (doc) {
                var col = createDocumentCardElement(doc);
                gridContainer.appendChild(col);
            });

            if (countBadge) {
                countBadge.textContent = documents.length + ' Approved Documents';
            }

            // Initialize pagination (10 entries per page)
            if (window.TablePaginator) {
                if (gridContainer.tablePaginator) {
                    try {
                        gridContainer.tablePaginator.update(true);
                    } catch (e) {
                        gridContainer.tablePaginator = new window.TablePaginator(gridContainer, { pageSize: 10 });
                    }
                } else {
                    gridContainer.tablePaginator = new window.TablePaginator(gridContainer, { pageSize: 10 });
                }
            }
            applyFilters();
        }

        function initUploadModal() {
            var uploadModalEl = document.getElementById('kbUploadModal');
            var uploadForm = document.getElementById('kbUploadForm');
            var fileInput = document.getElementById('kbFileInput');
            var dropZone = document.getElementById('kbDropZone');
            var filePreview = document.getElementById('kbFilePreview');
            var fileNameDisplay = document.getElementById('kbFileNameDisplay');
            var fileSizeDisplay = document.getElementById('kbFileSizeDisplay');
            var removeFileBtn = document.getElementById('btnRemoveKbFile');
            var titleInput = document.getElementById('kbDocumentTitleInput');
            var alertBox = document.getElementById('kbUploadAlert');
            var progressState = document.getElementById('kbUploadProgressState');
            var submitBtn = document.getElementById('btnSubmitKbUpload');
            var cancelBtn = document.getElementById('btnCancelKbUpload');
            var closeBtn = document.getElementById('btnKbUploadClose');

            if (!uploadForm || !fileInput) return;

            function showAlert(msg, isSuccess) {
                if (!alertBox) return;
                alertBox.className = 'alert ' + (isSuccess ? 'alert-success' : 'alert-danger') + ' py-2 px-3 small d-flex align-items-center gap-2';
                alertBox.innerHTML = (isSuccess ? '<i class="bi bi-check-circle-fill fs-5 text-success"></i>' : '<i class="bi bi-exclamation-triangle-fill fs-5 text-danger"></i>') +
                    '<div>' + msg + '</div>';
                alertBox.style.display = 'flex';
            }

            function clearAlert() {
                if (alertBox) {
                    alertBox.style.display = 'none';
                    alertBox.innerHTML = '';
                }
            }

            function handleFile(file) {
                clearAlert();
                if (!file) return;

                if (!file.name.toLowerCase().endsWith('.md')) {
                    showAlert('Invalid file format. Please upload a Markdown (<strong>.md</strong>) document only.', false);
                    resetFileSelection();
                    return;
                }

                if (file.size > 10 * 1024 * 1024) {
                    showAlert('File size exceeds the 10 MB limit.', false);
                    resetFileSelection();
                    return;
                }

                if (fileNameDisplay) fileNameDisplay.textContent = file.name;
                if (fileSizeDisplay) fileSizeDisplay.textContent = (file.size / 1024).toFixed(1) + ' KB';

                if (filePreview) filePreview.style.display = 'block';
                if (dropZone) dropZone.style.display = 'none';
                if (submitBtn) submitBtn.disabled = false;

                if (titleInput && !titleInput.value) {
                    var suggested = file.name.replace(/\.[^/.]+$/, '').replace(/[-_]/g, ' ');
                    titleInput.value = suggested.replace(/\b\w/g, function (l) { return l.toUpperCase(); });
                }
            }

            function resetFileSelection() {
                fileInput.value = '';
                if (filePreview) filePreview.style.display = 'none';
                if (dropZone) dropZone.style.display = 'block';
                if (submitBtn) submitBtn.disabled = true;
            }

            if (dropZone) {
                dropZone.addEventListener('click', function () {
                    fileInput.click();
                });

                ['dragenter', 'dragover'].forEach(function (eventName) {
                    dropZone.addEventListener(eventName, function (e) {
                        e.preventDefault();
                        e.stopPropagation();
                        dropZone.style.borderColor = 'var(--brand-blue, #2563eb)';
                        dropZone.style.background = 'rgba(37,99,235,0.06)';
                    });
                });

                ['dragleave', 'dragend', 'drop'].forEach(function (eventName) {
                    dropZone.addEventListener(eventName, function (e) {
                        e.preventDefault();
                        e.stopPropagation();
                        dropZone.style.borderColor = 'var(--border-soft)';
                        dropZone.style.background = 'var(--bg-surface-alt, rgba(0,0,0,0.02))';
                    });
                });

                dropZone.addEventListener('drop', function (e) {
                    var dt = e.dataTransfer;
                    var files = dt ? dt.files : null;
                    if (files && files.length > 0) {
                        fileInput.files = files;
                        handleFile(files[0]);
                    }
                });
            }

            fileInput.addEventListener('change', function () {
                if (fileInput.files && fileInput.files.length > 0) {
                    handleFile(fileInput.files[0]);
                } else {
                    resetFileSelection();
                }
            });

            if (removeFileBtn) {
                removeFileBtn.addEventListener('click', function () {
                    resetFileSelection();
                    clearAlert();
                });
            }

            uploadForm.addEventListener('submit', function (e) {
                e.preventDefault();
                clearAlert();

                if (!fileInput.files || fileInput.files.length === 0) {
                    showAlert('Please select a Markdown (.md) document to upload.', false);
                    return;
                }

                if (submitBtn) {
                    submitBtn.disabled = true;
                    submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status"></span> Uploading...';
                }
                if (cancelBtn) cancelBtn.disabled = true;
                if (closeBtn) closeBtn.disabled = true;
                if (progressState) progressState.style.display = 'block';

                var formData = new FormData(uploadForm);
                var tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
                var token = tokenEl ? tokenEl.value : '';

                fetch('/Knowledge/UploadPolicyDocument', {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': token
                    },
                    body: formData
                })
                .then(function (res) {
                    return res.json().then(function (data) {
                        if (!res.ok) {
                            throw new Error(data.message || 'Upload failed with HTTP ' + res.status);
                        }
                        return data;
                    });
                })
                .then(function (data) {
                    if (progressState) progressState.style.display = 'none';

                    var successMsg = data.message || 'Policy document successfully uploaded and indexed.';
                    if (data.chunksIndexed) {
                        successMsg += ' (' + data.chunksIndexed + ' knowledge chunks indexed)';
                    }
                    showAlert(successMsg, true);

                    if (data.document && gridContainer) {
                        var newCol = createDocumentCardElement(data.document);
                        gridContainer.insertBefore(newCol, gridContainer.firstChild);

                        var sec = data.document.section;
                        if (catFilter && sec) {
                            var exists = Array.from(catFilter.options).some(function (opt) {
                                return opt.value.toLowerCase() === sec.toLowerCase();
                            });
                            if (!exists) {
                                var opt = document.createElement('option');
                                opt.value = sec;
                                opt.textContent = sec;
                                catFilter.appendChild(opt);
                            }
                        }

                        applyFilters();
                    }

                    uploadForm.reset();
                    resetFileSelection();

                    if (submitBtn) {
                        submitBtn.disabled = false;
                        submitBtn.innerHTML = '<i class="bi bi-cloud-arrow-up-fill me-1"></i> Upload &amp; Ingest';
                    }
                    if (cancelBtn) cancelBtn.disabled = false;
                    if (closeBtn) closeBtn.disabled = false;

                    setTimeout(function () {
                        if (uploadModalEl && window.bootstrap && window.bootstrap.Modal) {
                            var modalInstance = window.bootstrap.Modal.getInstance(uploadModalEl);
                            if (modalInstance) modalInstance.hide();
                        }
                        clearAlert();
                    }, 2500);
                })
                .catch(function (err) {
                    console.error('[KnowledgeUpload] Error:', err);
                    if (progressState) progressState.style.display = 'none';
                    showAlert(err.message || 'An error occurred while uploading and ingesting the document.', false);

                    if (submitBtn) {
                        submitBtn.disabled = false;
                        submitBtn.innerHTML = '<i class="bi bi-cloud-arrow-up-fill me-1"></i> Upload &amp; Ingest';
                    }
                    if (cancelBtn) cancelBtn.disabled = false;
                    if (closeBtn) closeBtn.disabled = false;
                });
            });
        }

        initUploadModal();

        var kbTabBtn = document.querySelector('button[data-bs-target="#kbTab"]');
        if (kbTabBtn) {
            kbTabBtn.addEventListener('shown.bs.tab', function () {
                applyFilters();
            });
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () {
            initModalActions();
            initKnowledgeBaseWidget();
        });
    } else {
        initModalActions();
        initKnowledgeBaseWidget();
    }
})();
