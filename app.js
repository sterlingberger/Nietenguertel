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

    const filterU1 = document.getElementById('filter-u1');
    const filterU2 = document.getElementById('filter-u2');
    const filterU3 = document.getElementById('filter-u3');
    const filterU6 = document.getElementById('filter-u6');
    const headerRow = document.getElementById('header-row');
    const tbody = document.getElementById('table-body');
    const cardList = document.getElementById('card-list');

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
            const show = isVenueVisible(venueIdx) && isDateVisible(dateIso);
            card.style.display = show ? '' : 'none';
        });
    }

    // ── Alles aktualisieren ──────────────────────────────────────

    function applyFilters() {
        updateColumnVisibility();
        updateRowFilter();
        updateCardVisibility();
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
