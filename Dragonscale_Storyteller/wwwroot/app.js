// DOM Elements
const dropZone = document.getElementById('drop-zone');
const dropZoneActive = document.getElementById('drop-zone-active');
const fileInput = document.getElementById('file-input');
const selectFileBtn = document.getElementById('select-file-btn');
const uploadProgress = document.getElementById('upload-progress');
const progressBarFill = document.getElementById('progress-bar-fill');
const progressText = document.getElementById('progress-text');
const errorMessage = document.getElementById('error-message');
const errorText = document.getElementById('error-text');
const errorClose = document.getElementById('error-close');
const uploadSection = document.getElementById('upload-section');
const storySection = document.getElementById('story-section');
const storyTitle = document.getElementById('story-title');
const storyPhases = document.getElementById('story-phases');
const exportJsonBtn = document.getElementById('export-json-btn');
const exportPdfBtn = document.getElementById('export-pdf-btn');
const newStoryBtn = document.getElementById('new-story-btn');
const storyContentTemplate = document.getElementById('story-content-template');

// State
let currentStoryId = null;

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    initializeEventListeners();
});

function initializeEventListeners() {
    // File selection
    selectFileBtn.addEventListener('click', () => fileInput.click());
    fileInput.addEventListener('change', handleFileSelect);

    // Drag and drop
    dropZone.addEventListener('dragover', handleDragOver);
    dropZone.addEventListener('dragleave', handleDragLeave);
    dropZone.addEventListener('drop', handleDrop);

    // Error message
    errorClose.addEventListener('click', hideError);

    // Export buttons
    exportJsonBtn.addEventListener('click', handleExportJson);
    exportPdfBtn.addEventListener('click', handleExportPdf);

    // New story button
    newStoryBtn.addEventListener('click', resetToUpload);

    // Prevent default drag behavior on document
    document.addEventListener('dragover', (e) => e.preventDefault());
    document.addEventListener('drop', (e) => e.preventDefault());
}

// Drag and Drop Handlers
function handleDragOver(e) {
    e.preventDefault();
    e.stopPropagation();
    dropZone.classList.add('drag-over');
    dropZoneActive.hidden = false;
}

function handleDragLeave(e) {
    e.preventDefault();
    e.stopPropagation();
    dropZone.classList.remove('drag-over');
    dropZoneActive.hidden = true;
}

function handleDrop(e) {
    e.preventDefault();
    e.stopPropagation();
    dropZone.classList.remove('drag-over');
    dropZoneActive.hidden = true;

    const files = e.dataTransfer.files;
    if (files.length > 0) {
        handleFile(files[0]);
    }
}

function handleFileSelect(e) {
    const files = e.target.files;
    if (files.length > 0) {
        handleFile(files[0]);
    }
}

// File Processing
function handleFile(file) {
    // Validate file
    if (!file.type || file.type !== 'application/pdf') {
        showError('Bitte wählen Sie eine gültige PDF-Datei aus.');
        return;
    }

    if (file.size > 10 * 1024 * 1024) { // 10MB limit
        showError('Die Datei ist zu groß. Maximale Größe: 10MB.');
        return;
    }

    uploadFile(file);
}

// Upload File
async function uploadFile(file) {
    hideError();
    showUploadProgress();

    // Get configuration values
    const language = document.getElementById('language-select').value;
    const mood = document.getElementById('mood-select').value;
    const keywords = document.getElementById('keywords-input').value;

    const formData = new FormData();
    formData.append('file', file);
    formData.append('language', language);
    formData.append('mood', mood);
    if (keywords) {
        formData.append('keywords', keywords);
    }

    try {
        progressText.textContent = 'Wird hochgeladen...';
        progressBarFill.style.width = '30%';

        const response = await fetch('/api/storygenerator/upload', {
            method: 'POST',
            body: formData
        });

        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            throw new Error(errorData.message || `Upload fehlgeschlagen: ${response.statusText}`);
        }

        progressBarFill.style.width = '60%';
        progressText.textContent = 'Geschichte wird generiert...';

        const data = await response.json();

        if (!data.success || !data.storyId) {
            throw new Error(data.errorMessage || 'Fehler beim Generieren der Geschichte');
        }

        currentStoryId = data.storyId;

        progressBarFill.style.width = '100%';
        progressText.textContent = 'Fertig!';

        // Wait a moment before showing the story
        await new Promise(resolve => setTimeout(resolve, 500));

        await loadAndDisplayStory(data.storyId);

    } catch (error) {
        console.error('Upload error:', error);
        showError(error.message || 'Ein Fehler ist beim Hochladen aufgetreten.');
        hideUploadProgress();
    }
}

// Load and Display Story
async function loadAndDisplayStory(storyId) {
    try {
        const response = await fetch(`/api/storygenerator/${storyId}`);

        if (!response.ok) {
            throw new Error(`Fehler beim Laden der Geschichte: ${response.statusText}`);
        }

        const data = await response.json();

        if (!data.success || !data.story) {
            throw new Error(data.errorMessage || 'Geschichte konnte nicht geladen werden');
        }

        displayStory(data.story);

    } catch (error) {
        console.error('Load story error:', error);
        showError(error.message || 'Fehler beim Laden der Geschichte.');
    }
}

// Display Story
function displayStory(story) {
    // Hide upload section and progress
    uploadSection.hidden = true;
    uploadProgress.hidden = true;

    // Set story title
    storyTitle.textContent = story.title;

    // Clear previous content
    storyPhases.innerHTML = '';

    // Create continuous story content
    const storyContent = createContinuousStory(story);
    storyPhases.appendChild(storyContent);

    // Show story section
    storySection.hidden = false;

    // Scroll to story
    storySection.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

// Create Continuous Story (without phase separations)
function createContinuousStory(story) {
    const container = document.createElement('div');
    container.className = 'story-continuous';

    // Sort phases by order
    const sortedPhases = [...story.phases].sort((a, b) => a.order - b.order);

    // Build continuous narrative with interspersed images
    sortedPhases.forEach((phase, index) => {
        // Add image if available (every other image on alternating sides)
        if (phase.imageData) {
            const imageContainer = document.createElement('div');
            imageContainer.className = index % 2 === 0 ? 'story-image-left' : 'story-image-right';
            
            const img = document.createElement('img');
            img.src = `data:image/png;base64,${phase.imageData}`;
            img.alt = 'Story illustration';
            img.className = 'story-image';
            
            imageContainer.appendChild(img);
            container.appendChild(imageContainer);
        }

        // Add paragraph with summary text
        const paragraph = document.createElement('p');
        paragraph.className = 'story-paragraph';
        paragraph.textContent = phase.summary;
        container.appendChild(paragraph);
    });

    return container;
}



// Export JSON
async function handleExportJson() {
    if (!currentStoryId) return;

    try {
        exportJsonBtn.disabled = true;

        const response = await fetch(`/api/storygenerator/${currentStoryId}/export/json`);

        if (!response.ok) {
            throw new Error(`Export fehlgeschlagen: ${response.statusText}`);
        }

        const blob = await response.blob();
        downloadFile(blob, `story-${currentStoryId}.json`, 'application/json');

    } catch (error) {
        console.error('Export JSON error:', error);
        showError(error.message || 'Fehler beim Exportieren der JSON-Datei.');
    } finally {
        exportJsonBtn.disabled = false;
    }
}

// Export PDF
async function handleExportPdf() {
    if (!currentStoryId) return;

    try {
        exportPdfBtn.disabled = true;

        const response = await fetch(`/api/storygenerator/${currentStoryId}/export/pdf`);

        if (!response.ok) {
            throw new Error(`Export fehlgeschlagen: ${response.statusText}`);
        }

        const blob = await response.blob();
        downloadFile(blob, `story-${currentStoryId}.pdf`, 'application/pdf');

    } catch (error) {
        console.error('Export PDF error:', error);
        showError(error.message || 'Fehler beim Exportieren der PDF-Datei.');
    } finally {
        exportPdfBtn.disabled = false;
    }
}

// Download File
function downloadFile(blob, filename, mimeType) {
    const url = window.URL.createObjectURL(new Blob([blob], { type: mimeType }));
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
}

// Reset to Upload
function resetToUpload() {
    // Reset state
    currentStoryId = null;
    fileInput.value = '';

    // Hide story section
    storySection.hidden = true;

    // Show upload section
    uploadSection.hidden = false;

    // Clear story content
    storyPhases.innerHTML = '';
    storyTitle.textContent = '';

    // Hide error
    hideError();

    // Scroll to top
    window.scrollTo({ top: 0, behavior: 'smooth' });
}

// UI Helper Functions
function showUploadProgress() {
    uploadProgress.hidden = false;
    progressBarFill.style.width = '0%';
    selectFileBtn.disabled = true;
}

function hideUploadProgress() {
    uploadProgress.hidden = true;
    progressBarFill.style.width = '0%';
    selectFileBtn.disabled = false;
}

function showError(message) {
    errorText.textContent = message;
    errorMessage.hidden = false;
}

function hideError() {
    errorMessage.hidden = true;
    errorText.textContent = '';
}
