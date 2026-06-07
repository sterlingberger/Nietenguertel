document.addEventListener('DOMContentLoaded', function () {

    const rawVenues = window.VENUE_OBJECTS ?? [
        { Name: 'Arena', LocationFilter: ['U3'] },
        { Name: 'B72', LocationFilter: ['U6'] },
        { Name: 'Cafe Carina', LocationFilter: ['U6'] },
        { Name: 'Chelsea', LocationFilter: ['U6'] },
        { Name: 'Flucc', LocationFilter: ['U1', 'U2'] },
        { Name: 'Kramladen', LocationFilter: ['U6'] },
        { Name: 'Rhiz', LocationFilter: ['U6'] },
        { Name: 'Szene', LocationFilter: ['U3'] },
        { Name: 'Venster99', LocationFilter: ['U6'] },
        { Name: 'Viper Room', LocationFilter: ['U3'] }
    ];

    const VENUE_OBJECTS = rawVenues.map(v => ({
        name: v.Name ?? v.name,
        locations: Array.isArray(v.LocationFilter)
            ? v.LocationFilter
            : (v.LocationFilter ? [v.LocationFilter] : (v.locations ?? []))
    }));

    // ── Theme Toggle ─────────────────────────────────────────────

    const themeBtn = document.getElementById('theme-btn');
    const headerImg = document.getElementById('header-img');

    const DARK_IMG = 'data/snake_header_red.png';
    const LIGHT_IMG = 'data/snake_header_light.png';

    function getEffectiveTheme() {
        if (document.documentElement.dataset.theme) {
            return document.documentElement.dataset.theme;
        }
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }

    function applyTheme(theme) {
        document.documentElement.dataset.theme = theme;
        localStorage.setItem('theme', theme);
        if (themeBtn) themeBtn.textContent = theme === 'dark' ? 'bright mode' : 'dark mode';
        if (headerImg) headerImg.src = theme === 'light' ? LIGHT_IMG : DARK_IMG;
    }

    // Beim Start: data-theme wurde bereits im <head> gesetzt (kein FOWT).
    // Hier nur noch Button-Text synchronisieren.
    if (themeBtn) themeBtn.textContent = getEffectiveTheme() === 'dark' ? 'bright mode' : 'dark mode';

    if (themeBtn) {
        themeBtn.addEventListener('click', () => {
            const current = getEffectiveTheme();
            applyTheme(current === 'dark' ? 'light' : 'dark');
        });
    }

    // ── Filter State ─────────────────────────────────────────────

    const filterU1 = document.getElementById('filter-u1');
    const filterU2 = document.getElementById('filter-u2');
    const filterU3 = document.getElementById('filter-u3');
    const filterU6 = document.getElementById('filter-u6');
    const headerRow = document.getElementById('header-row');
    const tbody = document.getElementById('table-body');
    const cardList = document.getElementById('card-list');
    const genreBtn = document.getElementById('genre-btn');
    const genreWrap = document.getElementById('genre-wrap');

    // ── Genre-Filter State ───────────────────────────────────────

    const HARDCODED_TAGS = [
        { label: 'Blues', terms: ['blues', 'soul', 'gospel'] },
        { label: 'DJ', terms: ['dj'] },
        { label: 'Folk', terms: ['folk', 'singer-songwriter', 'bluegrass'] },
        { label: 'Funk', terms: ['funk'] },
        { label: 'Electronic', terms: ['electronic', 'electro', 'industrial'] },
        { label: 'Hardcore', terms: ['hardcore', 'metalcore', 'powerviolence'] },
        { label: 'Hip-Hop', terms: ['hip-hop', 'hip hop', 'hiphop', 'rap', 'r&b', 'rnb'] },
        { label: 'Indie', terms: ['indie'] },
        { label: 'Jazz', terms: ['jazz', 'swing', 'bebop', 'fusion'] },
        { label: 'Metal', terms: ['metal', 'thrash', 'doom'] },
        { label: 'Pop', terms: ['Pop'] },
        { label: 'Psych', terms: ['psych'] },
        { label: 'Punk', terms: ['punk', 'oi!', 'anarcho'] },
        { label: 'Reggae', terms: ['reggae', 'dub', 'dancehall'] },
        { label: 'Rock', terms: ['rock', 'punk', 'metal', 'stoner', 'doom', 'grunge', 'indie', 'alternative'] },
        { label: 'Ska', terms: ['ska'] },
        { label: 'Stoner', terms: ['stoner'] },
        { label: 'Techno', terms: ['techno', 'trance', 'dnb', 'drum and bass', "drum'n'bass"] },
    ];

    // label → terms Lookup (wird auch für custom tags befüllt)
    const tagTermsMap = new Map(HARDCODED_TAGS.map(t => [t.label, t.terms]));

    const activeTags = new Set();
    let genreMode = 'highlight'; // 'highlight' | 'filter'
    let allEventsData = []; // wird beim Rendern befüllt

    // ── Genre-Popover aufbauen ───────────────────────────────────

    const popover = document.createElement('div');
    popover.className = 'genre-popover';
    popover.id = 'genre-popover';
    popover.hidden = true;

    const chipsContainer = document.createElement('div');
    chipsContainer.className = 'genre-chips';
    chipsContainer.id = 'genre-chips-container';

    function createChip(label) {
        const btn = document.createElement('button');
        btn.className = 'genre-chip';
        btn.textContent = label;
        btn.addEventListener('click', () => {
            if (activeTags.has(label)) {
                activeTags.delete(label);
                btn.classList.remove('active');
            } else {
                activeTags.add(label);
                btn.classList.add('active');
            }
            applyFilters();
        });
        chipsContainer.appendChild(btn);
    }

    HARDCODED_TAGS.forEach(t => createChip(t.label));

    const inputRow = document.createElement('div');
    inputRow.className = 'genre-input-row';

    const tagInput = document.createElement('input');
    tagInput.type = 'text';
    tagInput.id = 'genre-input';
    tagInput.placeholder = 'Tag hinzufügen ...';
    tagInput.autocomplete = 'off';

    const addBtn = document.createElement('button');
    addBtn.textContent = '+';

    function addCustomTag() {
        const val = tagInput.value.trim();
        if (!val) return;
        tagInput.value = '';
        // custom tag: label = einziger suchterm
        if (!tagTermsMap.has(val)) tagTermsMap.set(val, [val]);
        createChip(val);
        activeTags.add(val);
        // aktiv schalten
        const chips = chipsContainer.querySelectorAll('.genre-chip');
        chips[chips.length - 1].classList.add('active');
        applyFilters();
    }

    addBtn.addEventListener('click', addCustomTag);
    tagInput.addEventListener('keydown', e => { if (e.key === 'Enter') addCustomTag(); });

    inputRow.appendChild(tagInput);
    inputRow.appendChild(addBtn);

    const modeToggle = document.createElement('div');
    modeToggle.className = 'genre-mode-toggle';

    const modeHighlight = document.createElement('button');
    modeHighlight.className = 'genre-mode-btn active';
    modeHighlight.dataset.mode = 'highlight';
    modeHighlight.textContent = 'hervorheben';

    const modeFilter = document.createElement('button');
    modeFilter.className = 'genre-mode-btn';
    modeFilter.dataset.mode = 'filter';
    modeFilter.textContent = 'andere ausblenden';

    [modeHighlight, modeFilter].forEach(btn => {
        btn.addEventListener('click', () => {
            genreMode = btn.dataset.mode;
            modeHighlight.classList.toggle('active', genreMode === 'highlight');
            modeFilter.classList.toggle('active', genreMode === 'filter');
            applyFilters();
        });
    });

    modeToggle.appendChild(modeHighlight);
    modeToggle.appendChild(modeFilter);

    const popoverHint = document.createElement('p');
    popoverHint.className = 'genre-popover-hint';
    popoverHint.textContent = 'Hinweis: als Grundlage für die Suche des Genres dient nur der Infotext der jeweiligen Veranstaltung. Hinter jedem Tag stecken gängige Schreibweisen oder verwandte Genres (Bsp.: Rock inkludiert Punk),' +
        'kommen diese im Text vor schlägt der Filter an. Für spezielle Suche eigenen Tag eingeben (damit lässt sich der Filter auch zweckentfremden für Bspw. Band- oder Veranstaltungsnamen).';
    popover.appendChild(popoverHint);

    popover.appendChild(chipsContainer);
    popover.appendChild(inputRow);
    popover.appendChild(modeToggle);
    genreWrap.appendChild(popover);

    // ── Popover öffnen/schließen ─────────────────────────────────

    genreBtn.addEventListener('click', e => {
        e.stopPropagation();
        popover.hidden = !popover.hidden;
    });

    document.addEventListener('click', e => {
        if (!genreWrap.contains(e.target)) {
            popover.hidden = true;
        }
    });

    // ── Genre-Hilfsfunktionen ────────────────────────────────────

    function isGenreMatch(eventIdx) {
        if (activeTags.size === 0) return true;
        const e = allEventsData[eventIdx];
        if (!e) return true;
        const info = (e.Info || e.InfoShort || '').toLowerCase();
        return [...activeTags].some(label => {
            const terms = tagTermsMap.get(label) || [label];
            return terms.some(term => info.includes(term.toLowerCase()));
        });
    }

    function updateGenreFilter() {
        const hasActive = activeTags.size > 0;
        genreBtn.classList.toggle('active', hasActive);

        // Reset
        document.querySelectorAll('.event-entry').forEach(el => {
            el.classList.remove('genre-match');
            el.style.display = '';
        });
        tbody.classList.remove('genre-active');
        if (cardList) {
            cardList.querySelectorAll('.card').forEach(c => c.classList.remove('genre-match'));
            cardList.classList.remove('genre-active');
        }

        if (!hasActive) return;

        // Event-entries
        document.querySelectorAll('.event-entry').forEach(el => {
            const idx = parseInt(el.dataset.eventIdx, 10);
            if (isNaN(idx)) return;
            if (isGenreMatch(idx)) {
                el.classList.add('genre-match');
            } else if (genreMode === 'filter') {
                el.style.display = 'none';
            }
        });

        // Cards (nur Klasse; display wird in updateCardVisibility gesetzt)
        if (cardList) {
            cardList.querySelectorAll('.card').forEach(card => {
                const idx = parseInt(card.dataset.eventIdx, 10);
                if (!isNaN(idx)) card.classList.toggle('genre-match', isGenreMatch(idx));
            });
        }

        if (genreMode === 'highlight') {
            tbody.classList.add('genre-active');
            if (cardList) cardList.classList.add('genre-active');
        }
    }

    // ── Hilfsfunktionen ─────────────────────────────────────────

    function parseISODate(iso) {
        const parts = String(iso).split('-').map(Number);
        if (parts.length < 3) return new Date(NaN);
        return new Date(parts[0], parts[1] - 1, parts[2]);
    }

    function startOfDay(d) {
        return new Date(d.getFullYear(), d.getMonth(), d.getDate());
    }

    function isVenueVisible(venueIndex) {
        const v = VENUE_OBJECTS[venueIndex];
        if (!v) return true;
        const locs = v.locations || [];
        return (filterU1.checked && locs.includes('U1')) ||
               (filterU2.checked && locs.includes('U2')) ||
               (filterU3.checked && locs.includes('U3')) ||
               (filterU6.checked && locs.includes('U6')) ||
               (locs.length === 0 && (filterU1.checked || filterU2.checked || filterU3.checked || filterU6.checked));
    }

    function isDateVisible(dateIso) {
        if (currentRowFilter === 'all') return true;
        const d = parseISODate(dateIso);
        if (isNaN(d)) return false;
        const sd = startOfDay(d);
        const today = startOfDay(new Date());
        if (currentRowFilter === 'today') return sd.getTime() === today.getTime();
        if (currentRowFilter === 'week') {
            const day = today.getDay();
            const diffToMonday = (day + 6) % 7;
            const start = startOfDay(new Date(today.getFullYear(), today.getMonth(), today.getDate() - diffToMonday));
            const end = startOfDay(new Date(start.getFullYear(), start.getMonth(), start.getDate() + 6));
            return sd >= start && sd <= end;
        }
        return true;
    }

    // ── Tabellen-Header ──────────────────────────────────────────

    function renderHeaders() {
        Array.from(headerRow.querySelectorAll('th.venue-col')).forEach(n => n.remove());
        VENUE_OBJECTS.forEach((v, idx) => {
            const th = document.createElement('th');
            th.className = 'venue-col';
            th.setAttribute('data-venue-index', idx);
            th.setAttribute('data-locations', v.locations.join(','));
            th.textContent = v.name;
            headerRow.appendChild(th);
        });
    }

    // ── Sichtbarkeit: Tabelle ────────────────────────────────────

    function updateColumnVisibility() {
        VENUE_OBJECTS.forEach((v, idx) => {
            const show = isVenueVisible(idx);
            const th = headerRow.querySelector(`th[data-venue-index="${idx}"]`);
            if (th) th.style.display = show ? '' : 'none';
            tbody.querySelectorAll(`td[data-venue-index="${idx}"]`).forEach(td => {
                td.style.display = show ? '' : 'none';
            });
        });
    }

    // ── Sichtbarkeit: Zeitfilter (Tabelle) ───────────────────────

    function updateRowFilter() {
        tbody.querySelectorAll('tr').forEach(tr => {
            const dateIso = tr.dataset.date;
            if (!dateIso) return;
            tr.style.display = isDateVisible(dateIso) ? '' : 'none';
        });
    }

    // ── Sichtbarkeit: Cards ──────────────────────────────────────

    function updateCardVisibility() {
        if (!cardList) return;
        cardList.querySelectorAll('.card').forEach(card => {
            const venueIdx = parseInt(card.dataset.venueIndex, 10);
            const dateIso = card.dataset.date;
            const eventIdx = parseInt(card.dataset.eventIdx, 10);
            const genreOk = activeTags.size === 0 || genreMode === 'highlight' || isGenreMatch(eventIdx);
            const show = isVenueVisible(venueIdx) && isDateVisible(dateIso) && genreOk;
            card.style.display = show ? '' : 'none';
        });
    }

    // ── Alles aktualisieren ──────────────────────────────────────

    function applyFilters() {
        updateColumnVisibility();
        updateRowFilter();
        updateCardVisibility();
        updateGenreFilter();
    }

    // ── Zeitfilter-Buttons ───────────────────────────────────────

    const rowButtons = Array.from(document.querySelectorAll('.row-btn'));
    let currentRowFilter = rowButtons.find(b => b.classList.contains('active'))?.dataset.filter ?? 'all';

    rowButtons.forEach(btn => {
        btn.addEventListener('click', () => {
            rowButtons.forEach(b => {
                b.classList.remove('active');
                b.setAttribute('aria-checked', 'false');
            });
            btn.classList.add('active');
            btn.setAttribute('aria-checked', 'true');
            currentRowFilter = btn.dataset.filter;
            applyFilters();
        });
    });

    // ── Icon-Filter ──────────────────────────────────────────────

    function updateIconState(icon, checked) {
        icon.classList.toggle('active', checked);
        icon.setAttribute('aria-pressed', String(checked));
    }

    document.querySelectorAll('.filter-icon').forEach(icon => {
        const cb = document.getElementById(icon.dataset.target);
        if (!cb) return;
        updateIconState(icon, cb.checked);
        icon.addEventListener('click', () => {
            cb.checked = !cb.checked;
            updateIconState(icon, cb.checked);
            applyFilters();
            cb.dispatchEvent(new Event('change', { bubbles: true }));
        });
        cb.addEventListener('change', () => updateIconState(icon, cb.checked));
    });

    [filterU1, filterU2, filterU3, filterU6].forEach(cb => {
        cb.addEventListener('change', applyFilters);
    });

    // ── Daten laden & rendern ────────────────────────────────────

    fetch('data/events.json')
        .then(r => r.json())
        .then(events => {
            renderHeaders();
            events.sort((a, b) => a.Date.localeCompare(b.Date));
            allEventsData = events;
            events.forEach((e, idx) => { e._idx = idx; });

            // Gruppieren nach Datum für Tabelle
            const byDate = {};
            for (const e of events) {
                if (!byDate[e.Date]) byDate[e.Date] = {};
                if (!byDate[e.Date][e.Venue]) byDate[e.Date][e.Venue] = [];
                byDate[e.Date][e.Venue].push(e);
            }

            tbody.innerHTML = '';
            if (cardList) cardList.innerHTML = '';

            for (const date of Object.keys(byDate).sort()) {
                const d = parseISODate(date);
                const weekday = d.toLocaleDateString('de-AT', { weekday: 'short' }).replace(/\.$/, '');
                const datePart = d.toLocaleDateString('de-AT', { day: '2-digit', month: '2-digit', year: '2-digit' });

                // ── Tabellenzeile ────────────────────────────────
                const tr = document.createElement('tr');
                tr.dataset.date = date;

                VENUE_OBJECTS.forEach((v, idx) => {
                    const td = document.createElement('td');
                    td.setAttribute('data-venue-index', idx);
                    const eventsForVenue = byDate[date][v.name] || [];

                    const dateLabel = document.createElement('div');
                    dateLabel.className = 'cell-date';
                    dateLabel.textContent = `${weekday} ${datePart}`;
                    td.appendChild(dateLabel);

                    eventsForVenue.forEach(e => {
                        const entry = document.createElement('div');
                        entry.className = 'event-entry';

                        const bandDiv = document.createElement('div');
                        bandDiv.className = 'band';
                        bandDiv.textContent = e.Artist || '';
                        if (e.Link) {
                            bandDiv.style.cursor = 'pointer';
                            bandDiv.title = 'Link öffnen';
                            bandDiv.addEventListener('click', () => {
                                try { window.open(e.Link, '_blank', 'noopener,noreferrer'); }
                                catch (err) { console.error('Konnte Link nicht öffnen', err); }
                            });
                        }

                        const infoDiv = document.createElement('div');
                        infoDiv.className = 'info';
                        infoDiv.textContent = e.InfoShort || '';

                        entry.dataset.eventIdx = e._idx;
                        entry.appendChild(bandDiv);
                        entry.appendChild(infoDiv);
                        td.appendChild(entry);
                    });
                    tr.appendChild(td);
                });

                tbody.appendChild(tr);

                // ── Cards ────────────────────────────────────────
                if (cardList) {
                    VENUE_OBJECTS.forEach((v, idx) => {
                        const eventsForVenue = byDate[date][v.name] || [];
                        eventsForVenue.forEach(e => {
                            const card = document.createElement('div');
                            card.className = 'card';
                            card.dataset.venueIndex = idx;
                            card.dataset.date = date;
                            card.dataset.eventIdx = e._idx;

                            card.innerHTML = `
                                <div class="card-meta">
                                    <span class="card-venue">${v.name}</span>
                                    <span class="card-date">${weekday} ${datePart}</span>
                                </div>
                                <div class="card-artist"></div>
                                <div class="card-info"></div>
                            `;

                            const artistEl = card.querySelector('.card-artist');
                            artistEl.textContent = e.Artist || '';
                            if (e.Link) {
                                artistEl.style.cursor = 'pointer';
                                artistEl.addEventListener('click', () => {
                                    try { window.open(e.Link, '_blank', 'noopener,noreferrer'); }
                                    catch (err) { console.error('Konnte Link nicht öffnen', err); }
                                });
                            }

                            card.querySelector('.card-info').textContent = e.InfoShort || '';
                            cardList.appendChild(card);
                        });
                    });
                }
            }

            applyFilters();
        })
        .catch(() => {
            tbody.innerHTML = '<tr><td colspan="5" style="color:red; text-align:center;">Fehler beim Laden der Daten.</td></tr>';
            if (cardList) cardList.innerHTML = '<p style="color:red; text-align:center;">Fehler beim Laden der Daten.</p>';
        });

    // ── Hilfs-Scrollleiste (Desktop) ─────────────────────────────

    (function () {
        const wrapper = document.querySelector('.table-wrapper');
        const table = document.getElementById('event-table');
        const hScroll = document.getElementById('h-scroll');
        const hInner = document.getElementById('h-scroll-inner');
        if (!wrapper || !table || !hScroll || !hInner) return;

        function updateHScroll() {
            const needH = table.scrollWidth > wrapper.clientWidth + 1;
            if (needH) {
                hInner.style.width = table.scrollWidth + 'px';
                hScroll.style.display = 'block';
            } else {
                hScroll.style.display = 'none';
            }
            hScroll.scrollLeft = wrapper.scrollLeft;
        }

        let raf = null;
        wrapper.addEventListener('scroll', () => {
            if (raf) cancelAnimationFrame(raf);
            raf = requestAnimationFrame(() => { hScroll.scrollLeft = wrapper.scrollLeft; raf = null; });
        });
        hScroll.addEventListener('scroll', () => {
            if (raf) cancelAnimationFrame(raf);
            raf = requestAnimationFrame(() => { wrapper.scrollLeft = hScroll.scrollLeft; raf = null; });
        });

        const ro = new ResizeObserver(updateHScroll);
        ro.observe(table);
        ro.observe(wrapper);
        window.addEventListener('load', updateHScroll);
        window.addEventListener('resize', updateHScroll);
        window.addEventListener('load', () => setTimeout(updateHScroll, 50));
    })();

}); // end DOMContentLoaded
