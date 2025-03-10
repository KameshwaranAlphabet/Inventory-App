// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function loadForm() {
    let formContainer = document.getElementById('formContainer');
    alert()
    // Show loading animation while fetching
    formContainer.innerHTML = `
            <div class="loading-container">
                <div class="spinner"></div>
            </div>`;


    fetch('@Url.Action("form", "Inventory")')
        .then(response => response.text())
        .then(html => {
            formContainer.innerHTML = html; // Load form inside formContainer
        })
        .catch(error => {
            formContainer.innerHTML = '<p>Error loading form.</p>';
            console.error('Error:', error);
        });
}
