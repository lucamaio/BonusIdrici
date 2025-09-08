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

    // Tabella Report

    var reportTable = $('#elencoReport').DataTable({
        paging: true,
        searching: true,
        ordering: true,
        responsive: true,
        language: { url: "https://cdn.datatables.net/plug-ins/2.0.8/i18n/it-IT.json" },
        dom: '<"row mb-3"<"col-sm-6"l><"col-sm-6 text-end"Bf>>t<"row mt-3"<"col-sm-6"i><"col-sm-6 text-end report"p>>',
        buttons: [ ],
    }); 

    reportTable.on('init', function () {
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