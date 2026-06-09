// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Dark Mode

// const toggleButton = document.getElementById("darkModeToggle");
//     toggleButton.addEventListener("click", () => {
//         document.body.classList.toggle("dark-mode");

//         // cambia icona luna/sole
//         const icon = toggleButton.querySelector("i");
//         if (document.body.classList.contains("dark-mode")) {
//             icon.classList.remove("bi-moon-fill");
//             icon.classList.add("bi-sun-fill");
//         } else {
//             icon.classList.remove("bi-sun-fill");
//             icon.classList.add("bi-moon-fill");
//         }
//     });

// Dark mode automatico in base al tema salvato in sessione

 // wwwroot/js/funzioni-trasversali.js

// document.addEventListener('DOMContentLoaded', () => {
//     const body = document.body;
//     const icon = document.getElementById('darkIcon');
//     const toggleButton = document.getElementById('toggleDarkMode');

//     // Valore fornito dal server (se presente)
//     const serverTheme = body.getAttribute('data-theme');

//     // Recupero preferenza locale o valore server
//     const savedTheme = localStorage.getItem('theme') || serverTheme || 'light';

//     // Applica il tema iniziale
//     if (savedTheme === 'dark') {
//         body.classList.add('dark-mode');
//         icon?.classList.replace('bi-moon-stars', 'bi-sun');
//     } else {
//         body.classList.remove('dark-mode');
//         icon?.classList.replace('bi-sun', 'bi-moon-stars');
//     }

//     // Gestione cambio tema
//     toggleButton?.addEventListener('click', () => {
//         body.classList.toggle('dark-mode');
//         const isDark = body.classList.contains('dark-mode');

//         if (isDark) {
//             localStorage.setItem('theme', 'dark');
//             icon?.classList.replace('bi-moon-stars', 'bi-sun');
//         } else {
//             localStorage.setItem('theme', 'light');
//             icon?.classList.replace('bi-sun', 'bi-moon-stars');
//         }
//     });
// });

// Load-Bar

 $(document).ready(function () {
    if (!$('#elencoDomande').length) {
        return;
    }

    $("#loadingBarContainer").show();
    var progress = 0;
    var interval = setInterval(function () {
        if (progress < 90) {
            progress += 10;
            $("#loadingBar").css("width", progress + "%");
        }
    }, 300);

    // Tabella Domande

    var domandeTable = $('#elencoDomande').DataTable({
        paging: true,
        searching: true,
        ordering: true,
        responsive: true,
        pageLength: 25,
        lengthMenu: [10, 25, 50, 100],
        language: { url: "https://cdn.datatables.net/plug-ins/2.0.8/i18n/it-IT.json" },
        dom: '<"d-flex justify-content-between align-items-center mb-3"l f>t<"row mt-3"<"col-sm-6"i><"col-sm-6 text-end"p>>',
        buttons: [],
        drawCallback: function (settings) {
            var api = this.api();
            var pages = api.page.info().pages;
            if (pages <= 1) {
                $(api.table().container()).find('.dataTables_paginate').hide();
                $(api.table().container()).find('.dataTables_length').hide();
            } else {
                $(api.table().container()).find('.dataTables_paginate').show();
                $(api.table().container()).find('.dataTables_length').show();
            }
        }
    }); 

    domandeTable.on('init', function () {
        clearInterval(interval);
        $("#loadingBar").css("width", "100%");
        setTimeout(function () {
            $("#loadingBarContainer").fadeOut();
            $("#tableWrapper").fadeIn(); 
        }, 500);
    });

});

(function (window, $) {
    if (!$) {
        return;
    }

    window.initDataTableWithLoader = function (selector, options) {
        var $table = $(selector);

        if (!$table.length || !$.fn.DataTable) {
            return null;
        }

        if ($.fn.DataTable.isDataTable($table)) {
            return $table.DataTable();
        }

        var $target = $table.closest('.table-responsive');
        if (!$target.length) {
            $target = $table;
        }

        var $loader = $('<div class="table-loader" role="status" aria-live="polite">' +
            '<div class="table-loader-track"><span></span></div>' +
            '<div class="table-loader-label"><i class="bi bi-hourglass-split"></i><span>Caricamento tabella...</span></div>' +
            '</div>');

        $loader.insertBefore($target);
        $target.addClass('table-loader-target').hide();

        var progress = 0;
        var interval = setInterval(function () {
            if (progress < 90) {
                progress += 10;
                $loader.find('.table-loader-track span').css('width', progress + '%');
            }
        }, 180);

        var settings = $.extend(true, {}, options || {});
        var originalInitComplete = settings.initComplete;

        settings.initComplete = function (settingsObj, json) {
            clearInterval(interval);
            $loader.find('.table-loader-track span').css('width', '100%');

            setTimeout(function () {
                $loader.fadeOut(160, function () {
                    $(this).remove();
                });
                $target.fadeIn(180);
            }, 220);

            if (typeof originalInitComplete === 'function') {
                originalInitComplete.call(this, settingsObj, json);
            }
        };

        return $table.DataTable(settings);
    };
})(window, window.jQuery);

document.addEventListener('DOMContentLoaded', function () {
    const searchInput = document.getElementById('searchEnte');
    const select = document.getElementById('selectedEnteId');
    const list = document.getElementById('filteredEnteList');
    const clearButton = document.getElementById('clearEnteSearch');
    const submitButton = document.getElementById('entitySubmitButton');
    const preview = document.getElementById('selectedEntePreview');

    if (!searchInput || !select || !list) {
        return;
    }

    const enti = Array.from(select.options)
        .filter(opt => opt.value !== "")
        .map(opt => ({ id: opt.value, nome: opt.text }));

    let activeIndex = -1;

    function resetSelection() {
        select.value = '';

        if (submitButton) {
            submitButton.disabled = true;
        }

        if (preview) {
            preview.classList.remove('is-selected');
            preview.replaceChildren();

            const icon = document.createElement('i');
            icon.className = 'bi bi-info-circle';

            const text = document.createElement('span');
            text.textContent = 'Nessun ente selezionato';

            preview.append(icon, text);
        }
    }

    function selectEnte(ente) {
        searchInput.value = ente.nome;
        select.value = ente.id;
        list.classList.add('d-none');
        activeIndex = -1;

        if (submitButton) {
            submitButton.disabled = false;
        }

        if (preview) {
            preview.classList.add('is-selected');
            preview.replaceChildren();

            const icon = document.createElement('i');
            icon.className = 'bi bi-check-circle';

            const text = document.createElement('span');
            const strong = document.createElement('strong');
            strong.textContent = ente.nome;

            text.append('Ente selezionato: ', strong, ' (ID ' + ente.id + ')');
            preview.append(icon, text);
        }
    }

    function renderResults(query) {
        const normalized = query.trim().toLowerCase();
        list.innerHTML = '';
        activeIndex = -1;

        if (!normalized) {
            list.classList.add('d-none');
            resetSelection();
            return;
        }

        const filtered = enti
            .filter(e => e.nome.toLowerCase().includes(normalized) || e.id.toLowerCase().includes(normalized))
            .slice(0, 12);

        if (filtered.length === 0) {
            const empty = document.createElement('li');
            empty.className = 'entity-results-empty';
            empty.textContent = 'Nessun ente trovato';
            list.appendChild(empty);
            list.classList.remove('d-none');
            resetSelection();
            return;
        }

        filtered.forEach(ente => {
            const item = document.createElement('li');
            const name = document.createElement('span');
            const id = document.createElement('small');

            name.textContent = ente.nome;
            id.textContent = 'ID ' + ente.id;
            item.append(name, id);

            item.addEventListener('mousedown', function (event) {
                event.preventDefault();
                selectEnte(ente);
            });
            list.appendChild(item);
        });

        list.classList.remove('d-none');
        resetSelection();
    }

    searchInput.addEventListener('input', function () {
        renderResults(this.value);
    });

    searchInput.addEventListener('keydown', function (event) {
        const items = Array.from(list.querySelectorAll('li:not(.entity-results-empty)'));

        if (!items.length || list.classList.contains('d-none')) {
            return;
        }

        if (event.key === 'ArrowDown') {
            event.preventDefault();
            activeIndex = (activeIndex + 1) % items.length;
        } else if (event.key === 'ArrowUp') {
            event.preventDefault();
            activeIndex = (activeIndex - 1 + items.length) % items.length;
        } else if (event.key === 'Enter' && activeIndex >= 0) {
            event.preventDefault();
            items[activeIndex].dispatchEvent(new MouseEvent('mousedown'));
            return;
        } else if (event.key === 'Escape') {
            list.classList.add('d-none');
            return;
        } else {
            return;
        }

        items.forEach(item => item.classList.remove('active'));
        items[activeIndex].classList.add('active');
    });

    if (clearButton) {
        clearButton.addEventListener('click', function () {
            searchInput.value = '';
            list.classList.add('d-none');
            resetSelection();
            searchInput.focus();
        });
    }

    document.addEventListener('click', function (event) {
        const clickedClearButton = clearButton && clearButton.contains(event.target);

        if (!searchInput.contains(event.target) && !list.contains(event.target) && !clickedClearButton) {
            list.classList.add('d-none');
        }
    });
});
