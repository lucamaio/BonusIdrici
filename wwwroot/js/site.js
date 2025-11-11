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

document.addEventListener('DOMContentLoaded', function () {
    const searchInput = document.getElementById('searchEnte');
    const select = document.getElementById('selectedEnteId');
    const list = document.getElementById('filteredEnteList');

    // Popola array degli enti
    const enti = Array.from(select.options)
        .filter(opt => opt.value !== "")
        .map(opt => ({ id: opt.value, nome: opt.text }));

    searchInput.addEventListener('input', function () {
        const query = this.value.toLowerCase();
        list.innerHTML = '';

        if (query.length === 0) {
            list.classList.add('d-none');
            select.value = '';
            return;
        }

        const filtered = enti.filter(e => e.nome.toLowerCase().includes(query));
        if (filtered.length === 0) {
            list.classList.add('d-none');
            return;
        }

        filtered.forEach(e => {
            const li = document.createElement('li');
            li.textContent = e.nome;
            li.className = 'list-group-item list-group-item-action';
            li.addEventListener('click', () => {
                searchInput.value = e.nome;
                select.value = e.id;
                list.classList.add('d-none');
            });
            list.appendChild(li);
        });

        list.classList.remove('d-none');
    });

    // Nasconde lista se clicchi fuori
    document.addEventListener('click', function (e) {
        if (!searchInput.contains(e.target) && !list.contains(e.target)) {
            list.classList.add('d-none');
        }
    });
});