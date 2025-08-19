// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(document).ready(function () {
    // Mostra barra all'inizio
    $("#loadingBarContainer").show();
    var progress = 0;

    var interval = setInterval(function () {
        if (progress < 90) {
            progress += 10;
            $("#loadingBar").css("width", progress + "%");
        }
    }, 200);

    // Prima tabella: riepilogoTable
    var riepilogoTable = $('#riepilogoTable').DataTable({
        paging: true,
        searching: true,
        ordering: true,
        responsive: true,
        language: {
            url: "https://cdn.datatables.net/plug-ins/2.0.8/i18n/it-IT.json"
        },
        dom: '<"row mb-3"<"col-sm-6"l><"col-sm-6 text-end"Bf>>t<"row mt-3"<"col-sm-6"i><"col-sm-6 text-end"p>>',
        buttons: [
            {
                extend: 'collection',
                className: 'btn btn-sm btn-primary dropdown-toggle me-2',
                text: '<i class="bi bi-download me-1"></i> Export',
                buttons: [
                    { extend: 'copy', text: 'Copia' },
                    { extend: 'excel', text: 'Excel' },
                    { extend: 'pdf', text: 'PDF' },
                    { extend: 'print', text: 'Stampa' }
                ]
            },
            {
                text: '<i class="bi bi-plus-circle me-1"></i> Aggiungi',
                className: 'btn btn-sm btn-success',
                action: function () {
                    window.location.href = '@Url.Action("Create", "Utenze", new { idEnte = selectedEnteId })';
                }
            }
        ]
    });

    // Quando la prima tabella è pronta → completa la barra e mostra tabella
    riepilogoTable.on('init', function () {
        clearInterval(interval);
        $("#loadingBar").css("width", "100%");
        setTimeout(function () {
            $("#loadingBarContainer").fadeOut();
            $("#tableWrapper").fadeIn();
        }, 500);
    });

    // Seconda tabella: riepilogoTableReport
    $('#riepilogoTableReport').DataTable({
        paging: true,
        searching: true,
        ordering: true,
        responsive: true,
        language: {
            url: "https://cdn.datatables.net/plug-ins/2.0.8/i18n/it-IT.json"
        },
        dom: '<"row mb-3"<"col-sm-6"l><"col-sm-6 text-end"Bf>>t<"row mt-3"<"col-sm-6"i><"col-sm-6 text-end"p>>',
        buttons: [
            {
                extend: 'collection',
                className: 'btn btn-sm btn-primary dropdown-toggle me-2',
                text: '<i class="bi bi-download me-1"></i> Export',
                buttons: [
                    { extend: 'copy', text: 'Copia' },
                    { extend: 'excel', text: 'Excel' },
                    { extend: 'pdf', text: 'PDF' },
                    { extend: 'print', text: 'Stampa' }
                ]
            }
        ]
    });
});
